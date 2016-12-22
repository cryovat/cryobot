using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryobot.Data;
using Dapper;
using Discord;
using Discord.Commands;
using Discord.Modules;
using Microsoft.Extensions.Logging;

namespace Cryobot.Modules.Base
{
    public abstract class ModuleBase : IModule
    {
        private readonly DbConnectionFactory _connectionFactory;

        public string Name => GetType().Name.Replace("Module", "");
        public virtual ModuleFilter Filter => ModuleFilter.None;

        public abstract void Install(ModuleManager manager);

        protected ILogger Logger { get; }

        protected ModuleBase(DbConnectionFactory connectionFactory, ILoggerFactory loggerFactory)
        {
            _connectionFactory = connectionFactory;
            Logger = loggerFactory.CreateLogger(GetType());
        }

        protected SqlConnection CreateConnection()
        {
            return _connectionFactory.Create();
        }

        protected async Task RecordInteraction(Server server, User byUser, User towardsUser, UserInteractionType interactionType)
        {
            Logger.LogInformation("Recording user interaction ({0}) towards {1}:{2} by {3}:{4} on {5}", interactionType, byUser.Name, byUser.Discriminator, towardsUser, towardsUser.Discriminator, server.Name);

            using (var conn = _connectionFactory.Create())
            {
                await conn.OpenAsync().ConfigureAwait(false);

                using (var trans = conn.BeginTransaction())
                {
                    await conn.RecordUserAsync(byUser, trans).ConfigureAwait(false);
                    await conn.RecordUserAsync(towardsUser, trans).ConfigureAwait(false);

                    await conn.RecordInteractionAsync(server, byUser, towardsUser, interactionType, trans).ConfigureAwait(false);

                    trans.Commit();
                }
            }
        }

        protected static User GetUser(CommandEventArgs args, string subject)
        {
            return string.IsNullOrWhiteSpace(subject) ? args.User : args.Channel.FindUsers(subject).FirstOrDefault();
        }
    }
}
