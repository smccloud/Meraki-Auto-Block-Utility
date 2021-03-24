﻿using System;
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

        public Settings() { }

        public Settings(bool remote, string remoteComputer, string remoteUserName, string remotePassword, string merakiAPIKey)
        {
            this.Remote = remote;
            this.RemoteComputer = remoteComputer;
            this.RemoteUserName = remoteUserName;
            this.RemotePassword = remotePassword;
            this.MerakiAPIKey = merakiAPIKey;
        }
    }
}
