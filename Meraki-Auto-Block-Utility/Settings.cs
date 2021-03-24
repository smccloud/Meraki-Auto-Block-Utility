using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meraki_Auto_Block_Utility
{
    public class Settings
    {
        public bool Remote { get; set; }
        public string RemoteComputer { get; set; }
        public string RemoteUserName { get; set; }
        public string RemotePassword { get; set; }
        public string MerakiAPIKey { get; set; }
        public string Organization { get; set; }
        public string NetworkId { get; set; }
        public string SubnetsToIgnore { get; set; }

        public Settings() { }

        public Settings(bool remote,
            string remoteComputer,
            string remoteUserName,
            string remotePassword,
            string merakiAPIKey,
            string organization,
            string networkId,
            string subnetsToIgnore)
        {
            this.Remote = remote;
            this.RemoteComputer = remoteComputer;
            this.RemoteUserName = remoteUserName;
            this.RemotePassword = remotePassword;
            this.MerakiAPIKey = merakiAPIKey;
            this.Organization = organization;
            this.NetworkId = networkId;
            this.SubnetsToIgnore = subnetsToIgnore;
        }
    }
}
