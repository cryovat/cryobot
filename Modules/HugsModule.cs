using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryobot.Data;
using Cryobot.Modules.Base;
using Dapper;
using Discord.Commands;
using Discord.Modules;
using Microsoft.Extensions.Logging;
using Cryobot.Data.Model;

namespace Cryobot.Modules
{
    public class HugsModule : ModuleBase
    {
        public HugsModule(DbConnectionFactory connectionFactory, ILoggerFactory loggerFactory)
            : base(connectionFactory, loggerFactory)
        {
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands(string.Empty, c =>
            {
                c.CreateCommand("hug").Description("Hugs someone.").Parameter("hugee", ParameterType.Optional).Do(Hug);
                c.CreateCommand("hugstats")
                    .Description("Gets hug stats for user.")
                    .Parameter("hugee", ParameterType.Optional)
                    .Do(HugStats);
            });
        }

        private static readonly string[] Hugs =
        {
            "hugs $name",
            "gives $name a warm snuggle",
            "glomps $name",
            "tacklehugs $name",
            "pulls $name into a happy pile of fuzzy kittens and bunnies"
        };

        private async Task Hug(CommandEventArgs args)
        {
            var random = new Random();

            var hugee = args.GetArg("hugee");

            var mention = GetUser(args, hugee);

            if (mention == null)
            {
                await
                    args.Channel.SendMessage(
                        $"I don't know who that is, sorry {args.User.NicknameMention}... :crying_cat_face:");
                return;
            }

            await RecordInteraction(args.Server, args.User, mention, UserInteractionType.Hug).ConfigureAwait(false);

            if (mention.Id == args.Server.CurrentUser.Id)
            {
                var message = "*grooms herself and purrs!* :cat:";

                await args.Channel.SendMessage(message).ConfigureAwait(false);
            }
            else
            {
                var hug = Hugs.OrderBy(i => random.Next()).First();
                var message = $"*{hug.Replace("$name", mention.NicknameMention)}!*";

                await args.Channel.SendMessage(message).ConfigureAwait(false);
            }
        }

        private async Task HugStats(CommandEventArgs args)
        {
            var hugee = args.GetArg("hugee");

            var mention = GetUser(args, hugee);

            if (mention == null)
            {
                await
                    args.Channel.SendMessage(
                        $"I don't know who that is, sorry {args.User.NicknameMention}... :crying_cat_face:");
                return;
            }

            InteractionData data;

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync().ConfigureAwait(false);

                data = await conn.GetInteractionsAsync(args.Server, mention).ConfigureAwait(false);
            }

            var message = new StringBuilder();

            message.Append("**Hug stats for: **");
            message.Append(mention.NicknameMention);
            message.AppendLine();

            message.AppendLine("```");

            message.AppendFormat("Received: {0}", data.ReceivedCount);
            message.AppendLine();

            message.AppendFormat("Given: {0}", data.GivenCount);
            message.AppendLine();

            message.AppendLine();

            if (data.Users.Length > 0)
            {
                message.AppendLine("Biggest fans:");

                for (int i = 0; i < Math.Min(data.Users.Length, 7); i++)
                {
                    message.AppendFormat("{0}. {1} ({2})", i + 1, data.Users[i].DisplayName, data.Users[i].Count);
                    message.AppendLine();
                }
            }
            else
            {
                message.AppendFormat("Nobody has hugged {0} yet, show them some love!", mention.Nickname ?? mention.Name);
                message.AppendLine();
            }

            message.Append("```");

            await args.Channel.SendMessage(message.ToString()).ConfigureAwait(false);
        }
    }
}
