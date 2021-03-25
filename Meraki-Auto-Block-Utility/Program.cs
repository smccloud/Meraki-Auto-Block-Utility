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
        static string json;
        static Settings settings;
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
                settings = new Settings();
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
                            if (entry != null)
                            {
                                if (entry.Source == "MSExchangeFrontEndTransport" && entry.InstanceId == 2147746827)
                                {
                                    string[] temp = entry.Message.Split('[');
                                    string[] temp2 = temp[1].Split(']');
                                    bool add = false;
                                    IPAddress iPAddress = IPAddress.Parse(temp2[0]);
                                    foreach (var subnet in Subnets)
                                    {
                                        if (SubnetCheck.IsInSubnet(iPAddress, subnet))
                                        {
                                            add = true;
                                            break;
                                        }
                                    }
                                    if (!ips.Contains(temp2[0]) && !add)
                                    {
                                        ips.Add(temp2[0]);
                                    }
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
                        rules.Add(temp2);
                    }
                }

                json = JsonConvert.SerializeObject(L7FirewallRules.rules);
                json = json.Replace('"', '\'');
                string workingFile = Directory.GetCurrentDirectory() + "\\update.py";
                if (File.Exists(workingFile))
                {
                    File.Delete(workingFile);
                }
                using (StreamWriter sw = new StreamWriter(workingFile))
                {
                    sw.WriteLine("import meraki");
                    sw.WriteLine("dashboard = meraki.DashboardAPI(\"" + settings.MerakiAPIKey + "\")");
                    sw.WriteLine("response = dashboard.appliance.updateNetworkApplianceFirewallL7FirewallRules(\"" + settings.NetworkId + "\", rules=" + json + ")");
                    sw.WriteLine("print(response)");
                }
                /*ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = settings.PythonPath;
                start.Arguments = workingFile;
                start.UseShellExecute = true;// Do not use OS shell
                start.CreateNoWindow = false; // We don't need new window
                start.LoadUserProfile = true;
                Process.Start(start);

                //Console.ReadLine();
                if (File.Exists(workingFile))
                {
                    File.Delete(workingFile);
                }*/
            }
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
