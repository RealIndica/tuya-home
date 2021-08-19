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
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

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

        Thread movementControllerThread;

        bool loaded = false;

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int key);

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

            //Robot movement experiment
            try
            {
                if (m == 2)
                {
                    movementControllerThread.Start();
                }
                else
                {
                    movementControllerThread.Abort();
                }
            }
            catch { }
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

        private void loadCommands()
        {
            var values = Enum.GetValues(typeof(Kit.Command));
            foreach (Kit.Command c in values)
            {
                comboBox1.Items.Add(Enum.GetName(typeof(Kit.Command), c));
            }

            comboBox1.SelectedIndex = 0;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            manageButtonInteractivity(false);
            loadCommands();
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

            movementControllerThread = new Thread(() => { movementController(); });
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

        private Keys currentMove;
        private Keys lastMove;
        private bool keyDown = false;

        private void processMove(bool stop = false)
        {
            if (stop)
            {
                currentMove = Keys.None;
            }
            if (currentMove != lastMove)
            {
                switch (currentMove)
                {
                    case Keys.Up:
                        genericRoboHoover.MoveForward();
                        break;
                    case Keys.Down:
                        genericRoboHoover.MoveBackward();
                        break;
                    case Keys.Left:
                        genericRoboHoover.MoveLeft();
                        break;
                    case Keys.Right:
                        genericRoboHoover.MoveRight();
                        break;
                    case Keys.None:
                        genericRoboHoover.Stop();
                        break;
                }

            }

            lastMove = currentMove;
        }

        private bool readytoControl()
        {
            try
            {
                if (loaded && Form.ActiveForm == this)
                {
                    if (selectedDevice != null)
                    {
                        if (selectedDevice.name.ToLower().Contains("coredy"))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        private void movementController()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(10);
                try
                {
                    if (readytoControl())
                    {
                        if ((GetAsyncKeyState((int)Keys.Up) & 0x8000) > 0) //up
                        {
                            keyDown = true;
                            currentMove = Keys.Up;
                        }
                        else if ((GetAsyncKeyState((int)Keys.Down) & 0x8000) > 0) //down
                        {
                            keyDown = true;
                            currentMove = Keys.Down;
                        }
                        else if ((GetAsyncKeyState((int)Keys.Left) & 0x8000) > 0) //left
                        {
                            keyDown = true;
                            currentMove = Keys.Left;
                        }
                        else if ((GetAsyncKeyState((int)Keys.Right) & 0x8000) > 0) //right
                        {
                            keyDown = true;
                            currentMove = Keys.Right;
                        }
                        else //stop
                        {
                            keyDown = false;
                        }
                        processMove(!keyDown);
                    }
                }
                catch { }
            }
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private Kit.Command getEnumFromComboBox()
        {
            string selected = comboBox1.Text;
            var values = Enum.GetValues(typeof(Kit.Command));
            foreach (Kit.Command c in values)
            {
                string cur = Enum.GetName(typeof(Kit.Command), c);

                if (cur == selected)
                {
                    return c;
                }
            }

            return Kit.Command.DP_QUERY;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            try
            {
                Dictionary<string, object> Ginfo = new Dictionary<string, object>();
                Ginfo = AsyncContext.Run(() => currentDevice().Send(getEnumFromComboBox()));
                string jsonOut = JsonConvert.SerializeObject(Ginfo, Formatting.Indented);
                MessageBox.Show(jsonOut, "Result");
            }
            catch { }
        }

        private void menuItem4_Click(object sender, EventArgs e)
        {

        }

        private void menuItem5_Click(object sender, EventArgs e)
        {
            string placeholder = "0000000000000000000000000000000000000000000000000000000000000000";
            string inputBytesString = Microsoft.VisualBasic.Interaction.InputBox("Enter Bytes", "Decode Payload Bytes", placeholder);
            if (inputBytesString != placeholder)
            {
                IEnumerable<string> strbytes = inputBytesString.ToLower().SplitInParts(2);
                List<byte> newBytes = new List<byte>();
                foreach (string s in strbytes)
                {
                    newBytes.Add(byte.Parse(s, System.Globalization.NumberStyles.HexNumber));
                }
                string decoded = placeholder;

                Kit.Request req = new Kit.Request();

                try
                {
                    decoded = req.Decrypt(newBytes.ToArray(), currentDevice());
                }
                catch { }

                decoded += "\r\n\r\n" + newBytes.Count.ToString() + " bytes";

                MessageBox.Show(decoded, "Result");
            }
        }

        private void menuItem7_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter DPS", "DPS Set", "Key:Value");
            if (input != "Key:Value")
            {
                try
                {
                    string Key = input.Split(':')[0];
                    object Value = stringToType(input.Split(':')[1]);

                    Dictionary<string, object> send = new Dictionary<string, object>();
                    send.Add(Key, Value);
                    AsyncContext.Run(() => currentDevice().Set(send));
                    updateEditor();
                }
                catch { }
            }
        }

        private void menuItem8_Click(object sender, EventArgs e)
        {

        }

        enum BruteMode
        {
            boolTrue = 0,
            boolFalse = 1,
            randomNumber = 2
        }

        private void dpsBrute(BruteMode b)
        {
            Random rnd = new Random();
            new Thread(() => {
                try
                {
                    Kit.Device d = currentDevice();
                    for (int i = 0; i < 200; i++)
                    {
                        Dictionary<string, object> send = new Dictionary<string, object>();

                        switch (b)
                        {
                            case BruteMode.boolTrue:
                                send.Add(i.ToString(), true);
                                break;
                            case BruteMode.boolFalse:
                                send.Add(i.ToString(), false);
                                break;
                            case BruteMode.randomNumber:
                                send.Add(i.ToString(), rnd.Next(0, 100000));
                                break;
                        }

                        AsyncContext.Run(() => d.Set(send));
                    }
                } catch (Exception e) { Console.WriteLine(e.Message); }
                this.Invoke(new MethodInvoker(updateEditor));
                MessageBox.Show("Attempted to reveal all values!", "Success");
            }).Start();
        }

        private void menuItem9_Click(object sender, EventArgs e)
        {
            dpsBrute(BruteMode.boolTrue);
        }

        private void menuItem10_Click(object sender, EventArgs e)
        {
            dpsBrute(BruteMode.boolFalse);
        }

        private void menuItem11_Click(object sender, EventArgs e)
        {
            dpsBrute(BruteMode.randomNumber);
        }
    }
}