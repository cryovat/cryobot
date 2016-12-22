using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cryobot.Data;
using Cryobot.Modules.Base;
using Discord.Commands;
using Discord.Modules;
using GeoDataSource;
using Microsoft.Extensions.Logging;

namespace Cryobot.Modules
{
    public class LocalTimeModule : ModuleBase
    {
        private readonly GeoData _geo = GeoData.Current;

        private readonly Dictionary<string, string> _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "cet", "Europe/Oslo" },
            { "est", "America/New_York" },
            { "eastern", "America/New_York" },
            { "cst", "America/Chicago" },
            { "central", "America/Chicago" },
            { "pst", "America/Los_Angeles" },
            { "pacific", "America/Los_Angeles" },
        };

        public LocalTimeModule(DbConnectionFactory connectionFactory, ILoggerFactory loggerFactory) : base(connectionFactory, loggerFactory)
        {
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands(string.Empty, c =>
            {
                c.CreateCommand("time").Description("Show the local time.").Parameter("timezone", ParameterType.Multiple).Do(PrintTime);
            });
        }

        private async Task PrintTime(CommandEventArgs args)
        {
            var arg = string.Join(" ", args.Args);
            if (string.IsNullOrWhiteSpace(arg))
            {
                arg = "Oslo";
            }

            if (_aliases.ContainsKey(arg))
            {
                arg = _aliases[arg];
            }            
            
            var zone = _geo.TimeZone(arg);
            var display = zone == null ? default(string) : _geo.GeoNames.FirstOrDefault(n => n.TimeZoneId == zone.TimeZoneId)?.Name;

            if (zone == null)
            {
                foreach (var country in _geo.Countries)
                {
                    if (arg.EqualsAny(country.ISOAlpha2, country.ISOAlpha3, country.Name))
                    {
                        display = country.Name;
                        zone = _geo.TimeZones.FirstOrDefault(z => z.CountryCode.Equals(country.ISOAlpha2));
                        break;
                    }
                }
            }

            if (zone == null)
            {
                foreach (var name in _geo.GeoNames)
                {
                    if (arg.EqualsAny(name.AsciiName, name.TwoLetterName, name.Name) || arg.EqualsAny(name.AlternateNames))
                    {
                        var country = _geo.GetCountry(name.CountryCode);

                        display = country == null ? name.Name : $"{name.Name}, {country.Name}";
                        zone = _geo.TimeZone(name.TimeZoneId);
                        break;
                    }
                }
            }

            if (zone == null)
            {
                foreach (var name in _geo.GeoNames)
                {
                    if (arg.PartOfAny(name.AsciiName, name.TwoLetterName, name.Name) || arg.PartOfAny(name.AlternateNames))
                    {
                        var country = _geo.GetCountry(name.CountryCode);

                        display = country == null ? name.Name : $"{name.Name}, {country.Name}";
                        zone = _geo.TimeZone(name.TimeZoneId);
                        break;
                    }
                }
            }

            if (zone != null)
            {
                var time = DateTime.UtcNow.AddHours(zone.GMTOffSet);

                await args.Channel.SendMessage($"Hey {args.User.NicknameMention}! The :clock3:  in {display} is {time:HH:mm:ss} (UTC+{zone.GMTOffSet})!");
            }
            else
            {
                await args.Channel.SendMessage($"Sorry {args.User.NicknameMention}, I don't know where that is... :crying_cat_face:");
            }
        }
    }

    internal static class Extensions
    {
        public static bool EqualsAny(this string value, params string[] args)
        {
            if (args == null) return false;

            return args.Any(a => string.Equals(value, a, StringComparison.OrdinalIgnoreCase));
        }

        public static bool EqualsAny(this string value, IEnumerable<string> args)
        {
            if (args == null) return false;

            return args.Any(a => string.Equals(value, a, StringComparison.OrdinalIgnoreCase));
        }

        public static bool PartOfAny(this string value, params string[] args)
        {
            if (args == null) return false;

            return args.Any(a => a != null && a.StartsWith(value, StringComparison.OrdinalIgnoreCase));
        }

        public static bool PartOfAny(this string value, IEnumerable<string> args)
        {
            if (args == null) return false;

            return args.Any(a => a != null && a.StartsWith(value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
