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
using System.Diagnostics;

namespace Tuya_Home
{
    public partial class Main : Form
    {
        Tuya tuya = new Tuya();
        ImageList iconList = new ImageList();
        Tuya.TuyaDevice selectedDevice;
        Kit.Devices.GenericLight genericLight;
        Kit.Devices.GenericSwitch genericSwitch;
        Kit.Device genericDevice;

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
                if (ip != "null")
                {
                    tuya.devices[idx].ip = ip;
                    listView1.Items[idx].Text = dev.name + "\r\n" + ip;
                } else
                {
                    tuya.devices[idx].ip = "0.0.0.0";
                    listView1.Items[idx].Text = dev.name + "\r\n" + "0.0.0.0";
                }

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
                newDev.product_name = dev.product_name;
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

            return 2;
        }

        private void manageSelected()
        {
            localKeyBox.Text = selectedDevice.localKey;
            devIdBox.Text = selectedDevice.virtualID;
            macBox.Text = selectedDevice.mac;
            ipBox.Text = selectedDevice.ip;

            int m = socketMode();
            switch (m)
            {
                case 0: //smart socket
                    genericSwitch = new Kit.Devices.GenericSwitch
                    {
                        IP = selectedDevice.ip,
                        devId = selectedDevice.virtualID,
                        localKey = selectedDevice.localKey,
                    };
                    break;
                case 1: //smart light
                    genericLight = new Kit.Devices.GenericLight
                    {
                        IP = selectedDevice.ip,
                        devId = selectedDevice.virtualID,
                        localKey = selectedDevice.localKey,
                    };
                    break;
                case 2: //Unknown
                    genericDevice = new Kit.Device
                    {
                        IP = selectedDevice.ip,
                        devId = selectedDevice.virtualID,
                        localKey = selectedDevice.localKey,
                    };
                    break;
            }
        }

        private void manageButtonInteractivity(bool enabled)
        {
            foreach (Control c in this.Controls)
            {
                c.Enabled = enabled;
                foreach (Control d in c.Controls)
                {
                    d.Enabled = enabled;
                }
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            manageButtonInteractivity(false);
            if (!Debugger.IsAttached)
            {
                new Thread(() => { loadDevices(); loadIPs(); manageButtonInteractivity(true); }).Start();
            }
            else
            {
                loadDevices();
                loadIPs();
                manageButtonInteractivity(true);
            }           
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
                updateEditor();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int m = socketMode();
            switch (m)
            {
                case 0: //smart socket
                    genericSwitch.ToggleAuto(genericSwitch);
                    break;
                case 1: //smart light
                    genericLight.ToggleAuto(genericLight);
                    break;
                case 2: //Generic Device
                    genericDevice.ToggleAuto(genericDevice);
                    break;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int m = socketMode();
            switch (m)
            {
                case 0: //smart socket
                    genericSwitch.TurnOnAuto(genericSwitch);
                    break;
                case 1: //smart light
                    genericLight.TurnOnAuto(genericLight);
                    break;
                case 2: //Generic Device
                    genericDevice.TurnOnAuto(genericDevice);
                    break;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int m = socketMode();
            switch (m)
            {
                case 0: //smart socket
                    genericSwitch.TurnOffAuto(genericSwitch);
                    break;
                case 1: //smart light
                    genericLight.TurnOffAuto(genericLight);
                    break;
                case 2: //Generic Device
                    genericDevice.TurnOffAuto(genericDevice);
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
                    Ginfo = AsyncContext.Run(() => genericSwitch.Get(true));
                    break;
                case 1: //smart light
                    Ginfo = AsyncContext.Run(() => genericLight.Get(true));
                    break;
                case 2: //Generic Device
                    Ginfo = AsyncContext.Run(() => genericDevice.Get(true));
                    break;
            }
            string jsonOut = JsonConvert.SerializeObject(Ginfo, Formatting.Indented);
            MessageBox.Show(jsonOut, "Information");
        }

        private void addEditorItem(string key, string value, int y)
        {
            TextBox textBox = new TextBox();
            Label label = new Label();
            Panel panel = new Panel();

            panel.Controls.Add(textBox);
            panel.Controls.Add(label);

            panel.Location = new Point(0, y);
            label.Location = new Point(0, 0);
            textBox.Location = new Point(0, 15);

            panel.Size = new Size(editPanel.Width - 17, textBox.Size.Height + label.Size.Height);
            textBox.Size = new Size(panel.Size.Width - 5, textBox.Size.Height);

            label.Text = key;
            textBox.Text = value;
            textBox.Tag = value;

            editPanel.Controls.Add(panel);
        }

        private void updateEditor()
        {
            editPanel.Controls.Clear();
            editPanel.Update();

            if (selectedDevice.ip != "0.0.0.0")
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
                    case 2: //Generic Device
                        Ginfo = AsyncContext.Run(() => genericDevice.Get());
                        break;
                }
                int idx = 0;
                int y = 0;

                foreach (KeyValuePair<string, object> i in Ginfo)
                {
                    addEditorItem(i.Key, i.Value.ToString(), y);
                    y = y + 45;
                    ++idx;
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            updateEditor();
        }

        private object stringToType(string input)
        {
            input = input.ToLower().TrimEnd();

            if (input == "true" || input == "false")
            {
                return Convert.ToBoolean(input);
            }

            if (int.TryParse(input, out _))
            {
                return Convert.ToInt32(input);
            }

            return input;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                toolStripStatusLabel1.Text = "Sending Data";
                Dictionary<string, object> sendDic = new Dictionary<string, object>();
                foreach (Control param in editPanel.Controls)
                {
                    bool addToDic = false;
                    string key = "";
                    string value = "";
                    foreach (Control a in param.Controls)
                    {
                        if (a is Label)
                        {
                            Label label = (Label)a;
                            key = label.Text;
                        }

                        if (a is TextBox)
                        {
                            TextBox textbox = (TextBox)a;
                            value = textbox.Text;

                            if (a.Tag.ToString() != value)
                            {
                                addToDic = true;
                            }
                        }
                    }

                    if (addToDic)
                    {
                        sendDic.Add(key, stringToType(value));
                    }
                }

                if (sendDic.Count != 0)
                {
                    foreach (var k in sendDic)
                    {
                        Dictionary<string, object> tempdic = new Dictionary<string, object>();
                        tempdic.Add(k.Key, k.Value);

                        int m = socketMode();
                        switch (m)
                        {
                            case 0: //smart socket
                                AsyncContext.Run(() => genericSwitch.Set(tempdic));
                                break;
                            case 1: //smart light
                                AsyncContext.Run(() => genericLight.Set(tempdic));
                                break;
                            case 2: //Generic Device
                                AsyncContext.Run(() => genericDevice.Set(tempdic));
                                break;
                        }
                        System.Threading.Thread.Sleep(50);
                    }
                }
                toolStripStatusLabel1.Text = "Idle";
                this.Invoke(new MethodInvoker(updateEditor));
            }).Start();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            foreach (var device in tuya.devices)
            {
                Kit.Device curdev = new Kit.Device {
                    IP = device.ip,
                    protocolVersion = "3.3",
                    localKey = device.localKey,
                    devId = device.virtualID
                };
                if (curdev.IP != "0.0.0.0")
                {
                    curdev.TurnOnAuto(curdev);
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            foreach (var device in tuya.devices)
            {
                Kit.Device curdev = new Kit.Device
                {
                    IP = device.ip,
                    protocolVersion = "3.3",
                    localKey = device.localKey,
                    devId = device.virtualID
                };
                if (curdev.IP != "0.0.0.0")
                {
                    curdev.TurnOffAuto(curdev);
                };
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            selectedDevice.ip = ipBox.Text;
        }
    }
}

