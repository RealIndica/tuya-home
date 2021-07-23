using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Threading;
using System.IO;
using System.Net;
using Nito.AsyncEx;

namespace Tuya_Home
{
    public partial class Main : Form
    {
        Tuya tuya = new Tuya();
        ImageList iconList = new ImageList();
        Tuya.TuyaDevice selectedDevice;
        Kit.Devices.GenericLight genericLight;
        Kit.Devices.GenericSwitch genericSwitch;

        public Main()
        {
            InitializeComponent();
            iconList.ColorDepth = ColorDepth.Depth32Bit;
            iconList.ImageSize = new Size(128, 128);
        }

        private static Stream imageStreamURL(string url)
        {
            WebClient cl = new WebClient();
            byte[] data = cl.DownloadData(url);
            return new MemoryStream(data);
        }

        private void loadIPs()
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {
                toolStripStatusLabel1.Text = "Grabbing IPs . . .";

                int idx = 0;
                foreach (Tuya.TuyaDevice dev in tuya.devices.ToList())
                {
                    string ip = IPMacMapper.FindIPFromMacAddress(dev.mac);
                    tuya.devices[idx].ip = ip;
                    listView1.Items[idx].Text = dev.name + "\r\n" + ip;
                    idx++;
                }

                toolStripStatusLabel1.Text = "Idle";
            }));
        }

        private void loadDevices()
        {
            toolStripStatusLabel1.Text = "Loading Devices . . .";
            string devicesString = cli_wrapper.getDevicesJson(ConfigStore.apiKey, ConfigStore.apiSecret, ConfigStore.virtualKey);
            List<Tuya.apiDevice> devicesJson = JsonConvert.DeserializeObject<List<Tuya.apiDevice>>(devicesString);
            foreach (Tuya.apiDevice dev in devicesJson)
            {
                Tuya.TuyaDevice newDev = new Tuya.TuyaDevice();
                newDev.icon = dev.icon;
                newDev.localKey = dev.key;
                newDev.virtualID = dev.id;
                newDev.name = dev.name;
                newDev.mac = cli_wrapper.getDeviceMac(dev.id);
                newDev.ip = "0.0.0.0";
                newDev.product_name = dev.product_name;
                iconList.Images.Add(dev.id, Image.FromStream(imageStreamURL(dev.icon)));
                tuya.devices.Add(newDev);
            }

            this.BeginInvoke(new MethodInvoker(delegate
            {
                listView1.LargeImageList = iconList;

                int idx = 0;
                foreach (Tuya.TuyaDevice dev in tuya.devices)
                {
                    listView1.Items.Add(dev.name + "\r\n" + dev.ip, idx);
                    idx++;
                }
            }));

            loadIPs();
        }

        private int socketMode()
        {
            if (selectedDevice.product_name.Contains("Socket"))
            {
                return 0;
            }
            else if (selectedDevice.product_name.ToLower().Contains("rgbc") || selectedDevice.product_name.ToLower().Contains("light"))
            {
                return 1;
            }

            return -1;
        }

        private void manageSelected()
        {
            localKeyBox.Text = selectedDevice.localKey;
            devIdBox.Text = selectedDevice.virtualID;
            macBox.Text = selectedDevice.mac;

            int m = socketMode();
            switch (m)
            {
                case 0: //smart socket
                    genericSwitch = new Kit.Devices.GenericSwitch
                    {
                        IP = selectedDevice.ip,
                        devId = selectedDevice.virtualID,
                        localKey = selectedDevice.localKey,
                        gwId = selectedDevice.virtualID
                    };
                    break;
                case 1: //smart light
                    genericLight = new Kit.Devices.GenericLight
                    {
                        IP = selectedDevice.ip,
                        devId = selectedDevice.virtualID,
                        localKey = selectedDevice.localKey,
                        gwId = selectedDevice.virtualID
                    };
                    break;
            }
        }


        private void Main_Load(object sender, EventArgs e)
        {
            new Thread(() => { loadDevices(); }).Start();
        }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count >= 1)
            {
                int selectedItem = listView1.SelectedItems[0].Index;
                selectedDevice = tuya.devices[selectedItem];
                selectedLabel.Text = selectedDevice.name;
                manageSelected();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int m = socketMode();
            switch (m)
            {
                case 0: //smart socket
                    genericSwitch.Toggle();
                    break;
                case 1: //smart light
                    genericLight.Toggle();
                    break;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int m = socketMode();
            switch (m)
            {
                case 0: //smart socket
                    genericSwitch.TurnOn();
                    break;
                case 1: //smart light
                    genericLight.TurnOn();
                    break;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int m = socketMode();
            switch (m)
            {
                case 0: //smart socket
                    genericSwitch.TurnOff();
                    break;
                case 1: //smart light
                    genericLight.TurnOff();
                    break;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Dictionary<string, object> Ginfo = new Dictionary<string, object>();
            int m = socketMode();
            switch (m)
            {
                case 0: //smart socket
                    Ginfo = AsyncContext.Run(() => genericSwitch.Get());
                    break;
                case 1: //smart light
                    Ginfo = AsyncContext.Run(() => genericLight.Get());
                    break;
            }
            string jsonOut = JsonConvert.SerializeObject(Ginfo, Formatting.Indented);
            MessageBox.Show(jsonOut, "Information");
        }
    }
}
