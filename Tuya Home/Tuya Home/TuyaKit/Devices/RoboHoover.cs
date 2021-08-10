using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

namespace Tuya_Home.Kit.Devices
{
    [Serializable]
    public class RoboHoover : Device
    {
        public async void MoveForward()
        {
            Dictionary<string, object> send = new Dictionary<string, object>();
            send.Add("105", "MoveForward");
            await Set(send);
        }

        public async void MoveBackward()
        {
            Dictionary<string, object> send = new Dictionary<string, object>();
            send.Add("105", "MoveBackward");
            await Set(send);
        }

        public async void MoveLeft()
        {
            Dictionary<string, object> send = new Dictionary<string, object>();
            send.Add("105", "MoveLeft");
            await Set(send);
        }

        public async void MoveRight()
        {
            Dictionary<string, object> send = new Dictionary<string, object>();
            send.Add("105", "MoveRight");
            await Set(send);
        }

        public async void Stop()
        {
            Dictionary<string, object> send = new Dictionary<string, object>();
            send.Add("105", "stop");
            await Set(send);
        }

        public async void CleanAuto()
        {
            Dictionary<string, object> send = new Dictionary<string, object>();
            send.Add("102", "clean_auto");
            await Set(send);
        }

        public async void CleanSmallRoom()
        {
            Dictionary<string, object> send = new Dictionary<string, object>();
            send.Add("102", "clean_sroom");
            await Set(send);
        }

        public async void CleanEdge()
        {
            Dictionary<string, object> send = new Dictionary<string, object>();
            send.Add("102", "clean_wall");
            await Set(send);
        }

        public async void CleanSpot()
        {
            Dictionary<string, object> send = new Dictionary<string, object>();
            send.Add("102", "clean_spot");
            await Set(send);
        }

        public async void GoHome()
        {
            Dictionary<string, object> send = new Dictionary<string, object>();
            send.Add("102", "find_sta");
            await Set(send);
        }
    }
}
