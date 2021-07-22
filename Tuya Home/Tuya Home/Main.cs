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

namespace Tuya_Home
{
    public partial class Main : Form
    {
        Tuya tuya = new Tuya();
        ImageList iconList = new ImageList();
        Tuya.TuyaDevice selectedDevice;

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
                iconList.Images.Add(dev.id, Image.FromStream(imageStreamURL(dev.icon)));
                tuya.devices.Add(newDev);
            }

            listView1.LargeImageList = iconList;

            int idx = 0;
            foreach (Tuya.TuyaDevice dev in tuya.devices)
            {
                listView1.Items.Add(dev.name + "\r\n" + dev.ip, idx);
                idx++;
            }
            loadIPs();
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
            }
        }
    }
}
