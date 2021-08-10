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
        Kit.Devices.RoboHoover genericRoboHoover;

        Keys currentMove;
        bool keyDown = false;

        bool loaded = false;

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
                }
                else
                {
                    tuya.devices[idx].ip = "0.0.0.0";
                    listView1.Items[idx].Text = dev.name + "\r\n" + "0.0.0.0";
                }

                idx++;
            }

            toolStripStatusLabel1.Text = "Idle";
            loaded = true;
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
            else if (selectedDevice.product_name.ToLower().Contains("coredy"))
            {
                return 2;
            }

            return -1;
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
                    genericSwitch.Derived = genericSwitch;
                    listView1.SelectedItems[0].Tag = genericSwitch.getBase();
                    break;
                case 1: //smart light
                    genericLight = new Kit.Devices.GenericLight
                    {
                        IP = selectedDevice.ip,
                        devId = selectedDevice.virtualID,
                        localKey = selectedDevice.localKey,
                    };
                    genericLight.Derived = genericLight;
                    listView1.SelectedItems[0].Tag = genericLight.getBase();
                    break;
                case 2: //robo hoover
                    genericRoboHoover = new Kit.Devices.RoboHoover
                    {
                        IP = selectedDevice.ip,
                        devId = selectedDevice.virtualID,
                        localKey = selectedDevice.localKey,
                    };
                    genericRoboHoover.Derived = genericRoboHoover;
                    listView1.SelectedItems[0].Tag = genericRoboHoover.getBase();
                    break;
                case -1: //unknown
                    genericDevice = new Kit.Device
                    {
                        IP = selectedDevice.ip,
                        devId = selectedDevice.virtualID,
                        localKey = selectedDevice.localKey,
                    };
                    genericDevice.Derived = genericDevice;
                    listView1.SelectedItems[0].Tag = genericDevice.getBase();
                    break;
            }
        }

        private void updateDeviceControls()
        {
            ControlPanel.Controls.Clear();
            currentDevice().GenerateForm(ControlPanel);
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
                updateDeviceControls();
            }
        }

        private Kit.Device currentDevice()
        {
            Kit.Device obj = (Kit.Device)listView1.SelectedItems[0].Tag;
            return obj;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            currentDevice().ToggleAuto(currentDevice());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            currentDevice().TurnOnAuto(currentDevice());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            currentDevice().TurnOffAuto(currentDevice());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Dictionary<string, object> Ginfo = new Dictionary<string, object>();
            Ginfo = AsyncContext.Run(() => currentDevice().Get(true));
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
                Ginfo = AsyncContext.Run(() => currentDevice().Get());

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
            string realInput = input.TrimEnd();
            input = input.ToLower().TrimEnd();

            if (input == "true" || input == "false")
            {
                return Convert.ToBoolean(realInput);
            }

            if (int.TryParse(input, out _))
            {
                return Convert.ToInt32(realInput);
            }

            return realInput;
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
                        AsyncContext.Run(() => currentDevice().Set(tempdic));
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
                Kit.Device curdev = new Kit.Device
                {
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

        private void button10_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                toolStripStatusLabel1.Text = "Sending Data";
                Dictionary<string, object> sendDic = new Dictionary<string, object>();
                foreach (Control param in editPanel.Controls)
                {
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
                        }
                    }
                    sendDic.Add(key, stringToType(value));
                }

                AsyncContext.Run(() => currentDevice().Set(sendDic));

                toolStripStatusLabel1.Text = "Idle";
                this.Invoke(new MethodInvoker(updateEditor));
            }).Start();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            keyDown = true;
            if (loaded)
            {
                if (selectedDevice.name.ToLower().Contains("coredy"))
                {
                    if (keyData == Keys.Up) //Move forward
                    {
                        currentMove = keyData;
                        return true;
                    }

                    if (keyData == Keys.Down) //Move backward
                    {
                        currentMove = keyData;
                        return true;
                    }

                    if (keyData == Keys.Left) //Move left
                    {
                        currentMove = keyData;
                        return true;
                    }

                    if (keyData == Keys.Right) //Move right
                    {
                        currentMove = keyData;
                        return true;
                    }
                }
            }
            keyDown = false;
            // Call the base class
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}

