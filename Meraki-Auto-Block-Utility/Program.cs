using Meraki.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;


namespace Meraki_Auto_Block_Utility
{
    class Program
    {
        static Root L7FirewallRules;
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            string configFile = Directory.GetCurrentDirectory() + "\\config.xml";
            if (!File.Exists(configFile))
            {
                Console.WriteLine("Config file not found, exiting!!!!");
            }
            else
            {
                L7FirewallRules = new Root();
                Settings settings = new Settings();
                XmlSerializer mySerializer = new XmlSerializer(typeof(Settings));
                using (StreamReader streamReader = new StreamReader(configFile))
                {
                    settings = (Settings)mySerializer.Deserialize(streamReader);
                }

                await GetDevices(settings.MerakiAPIKey, settings.Organization, settings.NetworkId);
                //await GetDevices(settings.MerakiAPIKey);
                EventLog[] eventLogs;
                if (settings.Remote)
                {
                    eventLogs = EventLog.GetEventLogs(settings.RemoteComputer);
                }
                else
                {
                    eventLogs = EventLog.GetEventLogs();
                }
                EventLogEntryCollection eventLogEntries;
                long count = 1;
                List<string> ips = new List<string>();

                Console.WriteLine("Number of logs on computer: " + eventLogs.Length);

                foreach (EventLog log in eventLogs)
                {
                    if (log.Log == "Application")
                    {
                        Console.WriteLine("Log: " + log.Log);
                        eventLogEntries = log.Entries;
                        foreach (EventLogEntry entry in eventLogEntries)
                        {
                            Console.WriteLine("Checking entry " + count.ToString() + " of " + eventLogEntries.Count.ToString());
                            count++;
                            if (entry.Source == "MSExchangeFrontEndTransport" && entry.InstanceId == 2147746827)
                            {
                                string[] temp = entry.Message.Split('[');
                                string[] temp2 = temp[1].Split(']');
                                ips.Add(temp2[0]);
                            }
                        }
                    }
                }
                foreach(var ip in ips)
                {
                    Rules temp = new Rules("deny", "ipRange", ip + "/32");

                    if (!L7FirewallRules.rules.Contains(temp))
                    {
                        L7FirewallRules.rules.Add(temp);
                    }
                }
                string json = JsonConvert.SerializeObject(L7FirewallRules);
            }
        }

        static async Task GetDevices(string apiKey, string organization = "", string networkId = "")
        {
            var merakiClient = new MerakiClient(new MerakiClientOptions
            {
                ApiKey = apiKey
            });

            if (string.IsNullOrEmpty(organization))
            {
                var organizations = await merakiClient
                    .Organizations
                    .GetAllAsync()
                    .ConfigureAwait(false);
                var firstOrganization = organizations[0];
                organization = firstOrganization.Id.ToString();
            }

            if (string.IsNullOrEmpty(networkId))
            {
                var networks = await merakiClient
                    .Networks
                    .GetAllAsync(organization)
                    .ConfigureAwait(false);
                networkId = networks[0].Id.ToString();
            }

            var l7FirewallRules = await merakiClient
                .MxLayer7FirewallRules
                .GetNetworkL7FirewallRules(networkId)
                .ConfigureAwait(false);

            L7FirewallRules = JsonConvert.DeserializeObject<Root>(l7FirewallRules.ToString());
        }
    }
}
