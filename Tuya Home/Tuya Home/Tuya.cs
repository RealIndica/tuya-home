using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tuya_Home
{
    public class Tuya
    {
        public List<TuyaDevice> devices = new List<TuyaDevice>();

        public class apiDevice
        {
            public string name { get; set; }
            public string id { get; set; }
            public string key { get; set; }
            public string icon { get; set; }
            public string product_name { get; set; }
        }

        public class TuyaDevice
        {
            public string virtualID { get; set; }
            public string name { get; set; }
            public string product_name { get; set; }
            public string localKey { get; set; }
            public string icon { get; set; }
            public string mac { get; set; }
            public string ip { get; set; }
            public string protocol_version { get; set; }
        }
    }
}
