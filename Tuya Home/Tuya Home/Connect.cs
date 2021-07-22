using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Tuya_Home
{
    public partial class Connect : Form
    {
        private string configFile = "Config.json";
        private bool loadedConfig = false;

        public Connect()
        {
            InitializeComponent();
            loadConfig();
            configToInputs();
        }

        private void saveConfig()
        {
            ConfigStore.apiKey = apiKeyInput.Text;
            ConfigStore.apiSecret = apiSecretInput.Text;
            ConfigStore.virtualKey = virtualIDInput.Text;

            ConfigEntry con = new ConfigEntry();
            con.apiKey = ConfigStore.apiKey;
            con.apiSecret = ConfigStore.apiSecret;
            con.virtualKey = ConfigStore.virtualKey;
            string newJson = JsonConvert.SerializeObject(con, Formatting.Indented);
            File.WriteAllText(configFile, newJson);
        }

        private void loadConfig()
        {
            if (File.Exists(configFile))
            {
                ConfigEntry con = JsonConvert.DeserializeObject<ConfigEntry>(File.ReadAllText(configFile));
                ConfigStore.apiKey = con.apiKey;
                ConfigStore.apiSecret = con.apiSecret;
                ConfigStore.virtualKey = con.virtualKey;
                loadedConfig = true;
            }
        }

        private void configToInputs()
        {
            if (loadedConfig)
            {
                apiKeyInput.Text = ConfigStore.apiKey;
                apiSecretInput.Text = ConfigStore.apiSecret;
                virtualIDInput.Text = ConfigStore.virtualKey;
            }
        }

        private bool checkInstalled(string softwareName)
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall") ??
          Registry.LocalMachine.OpenSubKey(
              @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");

            if (key == null)
                return false;

            return key.GetSubKeyNames()
                .Select(keyName => key.OpenSubKey(keyName))
                .Select(subkey => subkey.GetValue("DisplayName") as string)
                .Any(displayName => displayName != null && displayName.Contains(softwareName));
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/codetheweb/tuyapi/blob/master/docs/SETUP.md");
        }

        private void button1_Click(object sender, EventArgs e)
        {           
            if (!checkInstalled("Node.js"))
            {
                MessageBox.Show("You need to install Node.js.\r\nPress OK to go to the download page", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                System.Diagnostics.Process.Start("https://nodejs.org/en/download/");
                return;
            }

            TextBox[] tex = this.Controls["groupBox1"].Controls.OfType<TextBox>().ToArray();
            foreach (TextBox t in tex)
            {
                if (t.Text == string.Empty)
                {
                    MessageBox.Show("Please fill in all the fields!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            string res = cli_wrapper.getDevicesJson(apiKeyInput.Text, apiSecretInput.Text, virtualIDInput.Text);
            
            if (res != "BAD")
            {
                saveConfig();
                Program.ConnectSuccess = true;
                this.Close();
            } 
            else
            {
                MessageBox.Show("You have entered some information correctly or you have no added devices!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Connect_Load(object sender, EventArgs e)
        {

        }
    }
}
