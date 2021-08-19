using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Tuya_Home
{
    public static class IPMacMapper
    {
        private static List<IPAndMac> list;

        private static bool initArp = false;

        private static StreamReader ExecuteCommandLine(String file, String arguments = "")
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.FileName = file;
            startInfo.Arguments = arguments;
            Process process = Process.Start(startInfo);

            return process.StandardOutput;
        }

        private static string[] GetLocalIPAddresses()
        {
            List<string> ret = new List<string>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ret.Add(ip.ToString());
                }
            }
            if (ret.Count > 0)
            {
                return ret.ToArray();
            } else
            {
                throw new Exception("No network adapters with an IPv4 address in the system!");
            }
        }

        private static void initArpTable()
        {
            if (!initArp)
            {
                string[] ips = GetLocalIPAddresses();
                foreach (string ip in ips)
                { 
                    int idx = ip.LastIndexOf('.');
                    string formatted = ip.Substring(0, idx);
                    var initStream = ExecuteCommandLine(Environment.CurrentDirectory + "//bin//arpPopulate.bat", formatted);
                }
                System.Threading.Thread.Sleep(5000);
                initArp = true;
            }
        }

        private static void InitializeGetIPsAndMac()
        {
            if (list != null)
                return;

            var arpStream = ExecuteCommandLine("arp", "-a");
            List<string> result = new List<string>();
            while (!arpStream.EndOfStream)
            {
                var line = arpStream.ReadLine().Trim();
                result.Add(line);
            }

            list = result.Where(x => !string.IsNullOrEmpty(x) && (x.Contains("dynamic") || x.Contains("static")))
                .Select(x =>
                {
                    string[] parts = x.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    return new IPAndMac { IP = parts[0].Trim(), MAC = parts[1].Trim() };
                }).ToList();
        }

        public static string FindIPFromMacAddress(string macAddress)
        {
            initArpTable();
            InitializeGetIPsAndMac();
            macAddress = macAddress.Replace(':', '-').ToLower();
            IPAndMac item = list.SingleOrDefault(x => x.MAC == macAddress);
            if (item == null)
                return "null";
            return item.IP;
        }

        public static string FindMacFromIPAddress(string ip)
        {
            initArpTable();
            InitializeGetIPsAndMac();
            IPAndMac item = list.SingleOrDefault(x => x.IP == ip);
            if (item == null)
                return null;
            return item.MAC;
        }

        private class IPAndMac
        {
            public string IP { get; set; }
            public string MAC { get; set; }
        }
    }
}