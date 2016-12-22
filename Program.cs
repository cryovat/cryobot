using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cryobot.Data;
using Cryobot.Modules;
using Cryobot.Modules.Base;
using Cryobot.Services;
using Discord;
using Discord.Commands;
using Discord.Modules;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cryobot
{
    class Program
    {
        private static readonly IServiceProvider ServiceProvider;
        private static readonly ILoggerFactory LoggerFactory;

        static Program()
        {
            var collection = new ServiceCollection();

            collection.AddLogging();

            collection.AddSingleton<DbConnectionFactory>();

            collection.AddSingleton<RecordKeeperService>();
            
            collection.AddSingleton<ModuleBase, LocalTimeModule>();
            collection.AddSingleton<ModuleBase, HugsModule>();
            collection.AddSingleton<ModuleBase, KittensModule>();

            ServiceProvider = collection.BuildServiceProvider();

            LoggerFactory = ServiceProvider.GetService<ILoggerFactory>();
            LoggerFactory.AddConsole(LogLevel.Debug);
        }

        static int Main(string[] args)
        {
            var token = ConfigurationManager.AppSettings["ApiToken"];

            var app = new CommandLineApplication();
            app.OnExecute(async () => await Run(token));

            return app.Execute(args);
        }

        private static async Task<int> Run(string token)
        {
            try
            {
                var logger = LoggerFactory.CreateLogger(typeof(Program));

                var b = new DiscordConfigBuilder
                {
                    LogHandler = LogHandler,
                    LogLevel = LogSeverity.Debug
                };

                logger.LogInformation("Starting bot.");

                using (var client = new DiscordClient(b))
                {
                    client.UsingCommands(c =>
                    {
                        c.PrefixChar = '.';
                        c.HelpMode = HelpMode.Public;
                    });

                    client.AddService<ModuleService>();
                    client.AddService(ServiceProvider.GetService<RecordKeeperService>());

                    foreach (var module in ServiceProvider.GetServices<ModuleBase>())
                    {
                        client.AddModule((IModule)module, module.Name, module.Filter);
                    }

                    try
                    {
                        //client.MessageReceived += ClientOnMessageReceived;

                        await client.Connect(token, TokenType.Bot);

                        client.SetGame(".help for a list of commands");
                        
                        logger.LogInformation("Bot initialized and connected.");

                        Console.ReadKey();

                        logger.LogInformation("Shutting down bot.");
                    }
                    finally
                    {
                        await client.Disconnect();
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static void LogHandler(object sender, LogMessageEventArgs args)
        {
            var logger = LoggerFactory.CreateLogger(args.Source);

            switch (args.Severity)
            {
                case LogSeverity.Error:
                    logger.LogError(args.Message);
                    break;
                case LogSeverity.Warning:
                    logger.LogWarning(args.Message);
                    break;
                case LogSeverity.Info:
                    logger.LogInformation(args.Message);
                    break;
                case LogSeverity.Verbose:
                    logger.LogTrace(args.Message);
                    break;
                default:
                    logger.LogDebug(args.Message);
                    break;
            }
        }

        private static void ClientOnMessageReceived(object sender, MessageEventArgs args)
        {
            if (!args.Message.IsMentioningMe()) return;

            args.Channel.SendMessage($"*notices {args.User.NicknameMention}-senpai*");
        }
    }
}
