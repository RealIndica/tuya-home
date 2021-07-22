using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tuya_Home
{
    public static class ConfigStore
    {
        public static string apiKey { get; set; }
        public static string apiSecret { get; set; }
        public static string virtualKey { get; set; }
    }

    public class ConfigEntry
    {
        public string apiKey { get; set; }
        public string apiSecret { get; set; }
        public string virtualKey { get; set; }
    }
}
