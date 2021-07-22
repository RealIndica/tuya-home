using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Web;
using System.Net.Http;
using Nito.AsyncEx;
using System.Security.Cryptography;

namespace Tuya_Home
{
    public static class cli_wrapper
    {
        public class Command
        {
            public string command { get; set; }
            private string result { get; set; }

            public void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
            {
                result += outLine.Data + "\r\n";
            }

            public void run()
            {
                sendCliCommand(command, this);
            }

            public string getOutput()
            {
                return result;
            }
        }

        private static string JsonPrettify(string json)
        {
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);
                return stringWriter.ToString();
            }
        }

        private static string fullPathCLI()
        {
            return  "\"" + Environment.CurrentDirectory + "//bin//CLI//cli.js\"" ;
        }

        private static void sendCliCommand(string command, Command context)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = "node";
            p.StartInfo.Arguments = fullPathCLI() + " " + command;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.OutputDataReceived += new DataReceivedEventHandler(context.OutputHandler);
            p.ErrorDataReceived += new DataReceivedEventHandler(context.OutputHandler);
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
        }

        public static string getDeviceMac(string deviceKey)
        {
            string cmd = "mac --api-key " + ConfigStore.apiKey + " --api-secret " + ConfigStore.apiSecret + " --virtual-key " + deviceKey;
            Command com = new Command();
            com.command = cmd;
            com.run();
            dynamic json = JsonConvert.DeserializeObject(com.getOutput());
            return json[0].mac.ToString();
        }

        public static string getDevicesJson(string apiKey, string apiSecret, string virtualDeviceKey)
        {
            string cmd = "quickwizard --api-key " + apiKey + " --api-secret " + apiSecret + " --virtual-key " + virtualDeviceKey;
            Command com = new Command();
            com.command = cmd;
            com.run();
            string res = "";
            try
            {
                res = JsonPrettify(com.getOutput());
            } catch
            {
                res = "BAD";
            }
            return res;
        }
    }
}
