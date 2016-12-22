using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryobot.Data;
using Dapper;
using Discord;
using Microsoft.Extensions.Logging;
using RestSharp.Extensions;

namespace Cryobot.Services
{
    public class RecordKeeperService : IService
    {
        private readonly DbConnectionFactory _factory;
        private readonly ILogger _logger;

        public RecordKeeperService(DbConnectionFactory factory, ILoggerFactory loggerFactory)
        {
            _factory = factory;
            _logger = loggerFactory.CreateLogger(typeof(RecordKeeperService));
        }

        public void Install(DiscordClient client)
        {
            client.ServerAvailable += ClientOnServerAvailable;
            client.ChannelUpdated += ClientOnChannelUpdated;
        }

        private async void ClientOnChannelUpdated(object sender, ChannelUpdatedEventArgs args)
        {
            _logger.LogInformation("Recording channel update for {0} on {1}", args.After.Name, args.Server.Name);

            try
            {
                using (var conn = _factory.Create())
                {
                    await conn.OpenAsync();

                    await conn.RecordChannelAsync(args.After).ConfigureAwait(false);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Error while recording channel update: {0}", ex.GetBaseException().Message);
            }
        }

        private async void ClientOnServerAvailable(object sender, ServerEventArgs args)
        {
            _logger.LogInformation("Recording server state for {0}", args.Server.Name);

            try
            {
                using (var conn = _factory.Create())
                {
                    await conn.OpenAsync();

                    using (var trans = conn.BeginTransaction())
                    {
                        await conn.RecordServerAsync(args.Server, trans).ConfigureAwait(false);

                        foreach (var channel in args.Server.AllChannels)
                        {
                            await conn.RecordChannelAsync(channel, trans).ConfigureAwait(false);
                        }

                        trans.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while recording server: {0}", ex.GetBaseException().Message);
            }
        }
    }
}
