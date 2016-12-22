using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cryobot.Data.Model;
using Dapper;
using Discord;

namespace Cryobot.Data
{
    public static class DbExtensions
    {
        public static Task RecordServerAsync(this SqlConnection conn, Server server, SqlTransaction trans = null)
        {
            return conn.ExecuteAsync(RecordServerSql, new
            {
                id = (decimal)server.Id,
                name = server.Name
            },
            trans);
        }

        public static Task RecordChannelAsync(this SqlConnection conn, Channel channel, SqlTransaction trans = null)
        {
            return conn.ExecuteAsync(RecordChannelSql, new
            {
                Id = (decimal)channel.Id,
                ServerId = (decimal)channel.Server.Id,
                channel.Name,
                channel.Topic,
                channel.IsPrivate,
                IsVoice = channel.Type == ChannelType.Voice
            },
            trans);
        }

        public static Task RecordUserAsync(this SqlConnection conn, User user, SqlTransaction trans = null)
        {
            return conn.ExecuteAsync(RecordUserSql, new
            {
                Id = (decimal)user.Id,
                user.Name,
                Discriminator = (int)user.Discriminator,
                user.Nickname,
                user.AvatarId,
                user.AvatarUrl,
                user.IsBot
            },
            trans);
        }

        public static Task RecordInteractionAsync(this SqlConnection conn, Server server, User byUser, User towardsUser, UserInteractionType type, SqlTransaction trans = null)
        {
            return conn.ExecuteAsync(RecordInteractionSql, new
            {
                ServerId = (decimal)server.Id,
                ByUserId = (decimal)byUser.Id,
                TowardsUserId = (decimal)towardsUser.Id,
                InteractionType = (int)type
            },
            trans);
        }

        public static async Task<InteractionData> GetInteractionsAsync(this SqlConnection conn, Server server, User user, SqlTransaction trans = null)
        {
            var reader = await conn.QueryMultipleAsync(CountInteractionsSql, new
            {
                ServerId = (decimal)server.Id,
                UserId = (decimal)user.Id,
                InteractionType = (int)UserInteractionType.Hug
            },
            trans)
            .ConfigureAwait(false);

            var data = await reader.ReadFirstAsync<InteractionData>().ConfigureAwait(false);
            data.Users = (await reader.ReadAsync<InteractionUser>().ConfigureAwait(false)).ToArray();

            return data;
        }

        #region SQL

        private const string RecordServerSql = @"
MERGE INTO dbo.DiscordServers WITH (HOLDLOCK) AS Dst
USING
(
	SELECT @id As Id, @name As Name
)
AS Src
ON Src.Id = Dst.Id 
WHEN MATCHED AND Src.Name <> Dst.Name THEN
UPDATE
	SET Dst.[Name] = Src.[Name], Dst.UpdatedDate = SYSDATETIMEOFFSET()
WHEN NOT MATCHED THEN
	INSERT (Id, [Name]) VALUES (Src.Id, Src.Name);
";

        private const string RecordChannelSql = @"
MERGE INTO dbo.DiscordChannels WITH (HOLDLOCK) AS Dst
USING
(
	SELECT @Id As Id, @ServerId as ServerId, @Name As [Name], @Topic As Topic, @IsPrivate As IsPrivate, @IsVoice As IsVoice
)
AS Src
ON Src.Id = Dst.Id
WHEN MATCHED AND Dst.[Name] <> Src.[Name] OR ISNULL(Dst.Topic, '') <> ISNULL(Src.Topic, '') OR Dst.IsPrivate <> Src.IsPrivate OR Dst.IsVoice <> Src.IsVoice THEN
UPDATE
	SET Dst.[Name] = Src.[Name], Dst.Topic = Src.Topic, Dst.IsPrivate = Src.IsPrivate, Dst.IsVoice = Src.IsVoice, Dst.UpdatedDate = SYSDATETIMEOFFSET()
WHEN NOT MATCHED THEN
	INSERT (Id, ServerId, [Name], Topic, IsPrivate, IsVoice) VALUES (Src.Id, Src.ServerId, Src.[Name], Src.Topic, Src.IsPrivate, Src.IsVoice);
";

        private const string RecordUserSql = @"
MERGE INTO dbo.DiscordUsers WITH (HOLDLOCK) AS Dst
USING
(
	SELECT @Id As Id
)
AS Src
ON Src.Id = Dst.Id
WHEN MATCHED AND ISNULL(Dst.Nickname, '') <> ISNULL(@Nickname, '') OR Dst.AvatarId <> @AvatarId OR Dst.AvatarUrl <> @AvatarUrl THEN
UPDATE
	SET Dst.Nickname = @Nickname, Dst.AvatarId = @AvatarId, Dst.AvatarUrl = @AvatarUrl, UpdatedDate = SYSDATETIMEOFFSET()
WHEN NOT MATCHED THEN
	INSERT (Id, [Name], Discriminator, Nickname, AvatarId, AvatarUrl, IsBot) VALUES (Src.Id, @Name, @Discriminator, @Nickname, @AvatarId, @AvatarUrl, @IsBot);
";

        private const string RecordInteractionSql = @"
MERGE INTO dbo.UserInteractions WITH (HOLDLOCK) AS Dst
USING
(
	SELECT @ServerId As ServerId, @ByUserId as ByUserId, @TowardsUserId As TowardsUserId, @InteractionType As InteractionType
)
AS Src
ON Src.ServerId = Dst.ServerId AND Src.ByUserId = Dst.ByUserId AND Src.TowardsUserId = Dst.TowardsUserId AND Src.InteractionType = Dst.InteractionType
WHEN MATCHED THEN
UPDATE
	SET Dst.InteractionCount = Dst.InteractionCount + 1, Dst.UpdatedDate = SYSDATETIMEOFFSET()
WHEN NOT MATCHED THEN
	INSERT (ServerId, ByUserId, TowardsUserId, InteractionType, InteractionCount) VALUES (Src.ServerId, Src.ByUserId, Src.TowardsUserId, Src.InteractionType, 1);
";

        public const string CountInteractionsSql = @"
SELECT
	(SELECT SUM(InteractionCount) FROM dbo.UserInteractions WHERE ServerId = @ServerId AND TowardsUserId = @UserId AND InteractionType = @InteractionType) As ReceivedCount,
	(SELECT SUM(InteractionCount) FROM dbo.UserInteractions WHERE ServerId = @ServerId AND ByUserId = @UserId AND InteractionType = @InteractionType) As GivenCount;

SELECT TOP 20
	u.Id As Id,
	ISNULL(u.NickName, u.[Name]) As DisplayName,
	i.InteractionCount AS [Count]
FROM dbo.UserInteractions i
INNER JOIN dbo.DiscordUsers u
	ON i.ByUserId = u.Id
	WHERE ServerId = @ServerId AND TowardsUserId = @UserId AND InteractionType = @InteractionType;
";

        #endregion
    }
}
