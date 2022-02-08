using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Reflection;

namespace Tuya_Home.Kit
{

    [Serializable]
    public class Device
    {
        // Network properties.
        public string IP;
        public int port = 6668;
        public string protocolVersion = "3.3";

        // Device properties.  
        public string name;
        public string devId;
        public string productId;
        public string localKey;

        public object Derived;




        #region Accessors

        public async Task<Dictionary<string, object>> Get(bool schema = false, Dictionary<string, object> dps = null)
        {
            int epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            Dictionary<string, object> nulldps = new Dictionary<string, object>();

            if (dps == null)
            {
                dps = nulldps;
            }

            // Get the response.
            JObject response;
            if (!schema)
            {
                response = await new Request().SendJSONObjectForCommandToDevice(
                new Dictionary<string, object>
                {
                    ["gwId"] = this.devId,
                    ["devId"] = this.devId,
                    ["t"] = epoch,
                    ["dps"] = dps,
                    ["uid"] = this.devId
                },
                Command.DP_QUERY,
                this, true);
                return response["dps"].ToObject<Dictionary<string, object>>();
            }
            else
            {
                response = await new Request().SendJSONObjectForCommandToDevice(
                new Dictionary<string, object>
                {
                    ["gwId"] = this.devId,
                    ["devId"] = this.devId,
                    ["t"] = epoch,
                    ["dps"] = dps,
                    ["uid"] = this.devId,
                    ["schema"] = true
                },
                Command.DP_QUERY,
                this, true);
                return response.ToObject<Dictionary<string, object>>();
            }
        }

        public async Task<Dictionary<string, object>> Set(Dictionary<string, object> dps)
        {

            int epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            JObject response = await new Request().SendJSONObjectForCommandToDevice(
                new Dictionary<string, object>
                {
                    ["devId"] = this.devId,
                    ["gwId"] = this.devId,
                    ["uid"] = "",
                    ["t"] = epoch,
                    ["dps"] = dps
                },
                Command.CONTROL,
                this,
                true);

            // Return (if any).
            return response.ToObject<Dictionary<string, object>>();
        }

        public async Task<Dictionary<string, object>> Send(Command cmd)
        {
            int epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            Dictionary<string, object> nulldps = new Dictionary<string, object>();
            // Get the response.
            JObject response;

            response = await new Request().SendJSONObjectForCommandToDevice(
            new Dictionary<string, object>
            {
                ["gwId"] = this.devId,
                ["devId"] = this.devId,
                ["t"] = epoch,
                ["dps"] = nulldps,
                ["uid"] = this.devId
            },
            cmd,
            this, true);
            return response.ToObject<Dictionary<string, object>>();
        }

        private async Task<string> getFirst(Device dev)
        {
            Dictionary<string, object> dps = await dev.Get();
            Console.WriteLine("Auto Power Key : " + dps.First().Key);
            return dps.First().Key;
        }


        public async void TurnOffAuto(Device dev)
        {
            string auto = await getFirst(dev);
            Dictionary<string, object> response = await dev.Set(
                new Dictionary<string, object>
                {
                    [auto] = false
                }
            );

            Log.Format("response: `{0}`", response);
        }

        public async void TurnOnAuto(Device dev)
        {
            string auto = await getFirst(dev);
            Dictionary<string, object> response = await dev.Set(
                new Dictionary<string, object>
                {
                    [auto] = true
                }
            );

            Log.Format("response: `{0}`", response);
        }

        public async void ToggleAuto(Device dev)
        {
            Dictionary<string, object> dps = await dev.Get();
            string auto = await getFirst(dev);

            bool isOn = (bool)dps[auto];
            if (isOn) TurnOffAuto(dev); else TurnOnAuto(dev);

        }

        #endregion

        public Device getBase()
        {
            return (Device)this;
        }

        public string getVersion()
        {
            return "3.3";
        }

        public void addEditorItem(string value, int y, Panel editPanel, MethodInfo method, object inst)
        {
            Button btn = new Button();
            btn.Location = new Point(0, y);

            btn.Size = new Size(editPanel.Size.Width - 20, btn.Size.Height);

            btn.Text = value;
            btn.Tag = value;

            btn.Click += new EventHandler(delegate (object o, EventArgs a) { method.Invoke(inst, null); });

            editPanel.Controls.Add(btn);
        }

        public void GenerateForm(Panel TargetControl)
        {
            int idx = 0;
            int y = 0;

            foreach (var method in Derived.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.Name != "GenerateForm" && method.GetParameters().Length == 0)
                {
                    string currentMethodName = method.Name;
                    addEditorItem(currentMethodName, y, TargetControl, method, Derived);
                    y = y + 25;
                    ++idx;
                }
            }
        }
    }
}