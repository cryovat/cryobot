using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryobot.Data;
using Cryobot.Modules.Base;
using Cryobot.Properties;
using Discord.Commands;
using Discord.Modules;
using Microsoft.Extensions.Logging;

namespace Cryobot.Modules
{
    public class KittensModule : ModuleBase
    {
        private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(0.5);
        private static readonly Random Random = new Random();

        private readonly object _lock = new object();
        private readonly Dictionary<ulong, DateTime> _last = new Dictionary<ulong, DateTime>();

        private static readonly byte[][] Kittens =
        {
            Resources.kitten1,
            Resources.kitten2,
            Resources.kitten3,
            Resources.kitten4,
            Resources.kitten5,
            Resources.kitten6,
        };

        public KittensModule(DbConnectionFactory connectionFactory, ILoggerFactory loggerFactory) : base(connectionFactory, loggerFactory)
        {
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands(string.Empty, c =>
            {
                c.CreateCommand("kitten").Description("Posts a picture of a kitten.").Do(Kitteh);
            });
        }

        public async Task Kitteh(CommandEventArgs args)
        {
            var ok = true;
            var now = DateTime.Now;
            var stamp = default(DateTime);
            byte[] kitten = null;

            lock (_lock)
            {
                if (_last.ContainsKey(args.Channel.Id))
                {
                    stamp = _last[args.Channel.Id];

                    if (now > stamp + Cooldown)
                    {
                        _last[args.Channel.Id] = now;
                    }
                    else
                    {
                        ok = false;
                    }
                }
                else
                {
                    _last[args.Channel.Id] = DateTime.Now;
                }

                if (ok)
                {
                    kitten = Kittens.OrderBy(r => Random.Next()).First();
                }
            }

            if (ok)
            {
                using (var ms = new MemoryStream(kitten))
                {
                    await args.Channel.SendFile("kitten.jpg", ms);
                }
            }
            else
            {
                await args.Channel.SendMessage($"*Please wait {(Cooldown - (now - stamp)).TotalSeconds:N0} seconds for the next kitten. I hate cooldowns, but trying not to get banned.*");
            }
        }
    }
}
