using Meraki.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Meraki.Api.Data;
using System.Net;

namespace Meraki_Auto_Block_Utility
{
    class Program
    {
        static Root L7FirewallRules;
        static List<string> Subnets;
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
                Subnets = new List<string>();
                Settings settings = new Settings();
                XmlSerializer mySerializer = new XmlSerializer(typeof(Settings));
                using (StreamReader streamReader = new StreamReader(configFile))
                {
                    settings = (Settings)mySerializer.Deserialize(streamReader);
                }
                string[] subnets = settings.SubnetsToIgnore.Split(',');
                foreach (var subnet in subnets)
                {
                    Subnets.Add(subnet);
                }
                await GetL7FirewallRules(settings.MerakiAPIKey, settings.Organization, settings.NetworkId);
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
                long count = 1;
                List<string> ips = new List<string>();

                Console.WriteLine("Number of logs on computer: " + eventLogs.Length);

                foreach (EventLog log in eventLogs)
                {
                    if (log.Log == "Application")
                    {
                        Console.WriteLine("Log: " + log.Log);
                        var eventLogEntries = new EventLogEntry[log.Entries.Count + 1000];
                        int amount = log.Entries.Count;
                        log.Entries.CopyTo(eventLogEntries, 0);
                        foreach (EventLogEntry entry in eventLogEntries)
                        {
                            Console.WriteLine("Checking entry " + count.ToString() + " of " + amount.ToString());
                            count++;
                            if (entry.Source == "MSExchangeFrontEndTransport" && entry.InstanceId == 2147746827 && entry != null)
                            {
                                string[] temp = entry.Message.Split('[');
                                string[] temp2 = temp[1].Split(']');
                                bool add = false;
                                IPAddress iPAddress = IPAddress.Parse(temp2[0]);
                                foreach(var subnet in Subnets)
                                {
                                    if(SubnetCheck.IsInSubnet(iPAddress,subnet))
                                    {
                                        add = true;
                                        break;
                                    }
                                }
                                if (!ips.Contains(temp2[0]) && add)
                                {
                                    ips.Add(temp2[0]);
                                }
                            }
                        }
                    }
                }
                List<Layer7FirewallRule> rules = new List<Layer7FirewallRule>();
                foreach(var rule in L7FirewallRules.rules)
                {
                    Layer7FirewallRule temp = new Layer7FirewallRule();
                    temp.Policy = Layer7FirewallRulePolicy.Deny;
                    temp.Type = Layer7FirewallRuleType.IpRange;
                    temp.Value = rule.value.ToString();
                    rules.Add(temp);
                }
                foreach (var ip in ips)
                {
                    Rules temp = new Rules("deny", "ipRange", ip + "/32");
                    if (!L7FirewallRules.rules.Contains(temp))
                    {
                        L7FirewallRules.rules.Add(temp);
                        Layer7FirewallRule temp2 = new Layer7FirewallRule();
                        temp2.Policy = Layer7FirewallRulePolicy.Deny;
                        temp2.Type = Layer7FirewallRuleType.IpRange;
                        temp2.Value = ip;
                    }
                }

                await SetL7FirewallRules(settings.MerakiAPIKey, settings.Organization, settings.NetworkId, rules);

            }
        }

        static async Task SetL7FirewallRules(string apiKey, string organization, string networkId, List<Layer7FirewallRule> rules)
        {
            var merakiClient = new MerakiClient(new MerakiClientOptions
            {
                ApiKey = apiKey
            });

            Layer7FirewallRulesUpdateRequest layer7FirewallRulesUpdateRequest = new Layer7FirewallRulesUpdateRequest();
            layer7FirewallRulesUpdateRequest.Rules.AddRange(rules);

            var l7FirewallRules = await merakiClient
                 .MxLayer7FirewallRules
                 .UpdateNetworkL7FirewallRules(networkId, layer7FirewallRulesUpdateRequest)
                 .ConfigureAwait(false);
             
        }

        static async Task GetL7FirewallRules(string apiKey, string organization = "", string networkId = "")
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
