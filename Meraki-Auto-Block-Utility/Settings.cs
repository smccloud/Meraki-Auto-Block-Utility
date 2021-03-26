using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meraki_Auto_Block_Utility
{
    public class Settings
    {
        /// <summary>
        /// Will we be getting logs from a remote computer?
        /// Please note, this is slower
        /// </summary>
        public bool Remote { get; set; }
        /// <summary>
        /// The name of the remote computer
        /// </summary>
        public string RemoteComputer { get; set; }
        /// <summary>
        /// Your Meraki API key, suggestion is to use one just for this app
        /// </summary>
        public string MerakiAPIKey { get; set; }
        /// <summary>
        /// Your organization in the Meraki Cloud
        /// </summary>
        public string Organization { get; set; }
        /// <summary>
        /// The NetworkID your security appliance is in
        /// </summary>
        public string NetworkId { get; set; }
        /// <summary>
        /// Comma delimited list of subnets we do not want to block (normally internal subnets)
        /// </summary>
        public string SubnetsToIgnore { get; set; }

        public Settings() { }

        public Settings(bool remote,
            string remoteComputer,
            string merakiAPIKey,
            string organization,
            string networkId,
            string subnetsToIgnore)
        {
            this.Remote = remote;
            this.RemoteComputer = remoteComputer;;
            this.MerakiAPIKey = merakiAPIKey;
            this.Organization = organization;
            this.NetworkId = networkId;
            this.SubnetsToIgnore = subnetsToIgnore;
        }
    }
}
