using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meraki_Auto_Block_Utility
{
    public class Rules
    {
        public string policy { get; set; }
        public string type { get; set; }
        public object value { get; set; }
        
        public Rules() { }

        public Rules(string policy, string type, object value)
        {
            this.policy = policy;
            this.type = type;
            this.value = value;
        }
    }

    public class Root
    {
        public List<Rules> rules { get; set; }

        public Root()
        {
            rules = new List<Rules>();
        }
    }
}
