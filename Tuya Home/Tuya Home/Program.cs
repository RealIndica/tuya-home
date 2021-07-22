using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Tuya_Home
{
    static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        public static bool ConnectSuccess = false;
        private static bool debugConsole = true;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (debugConsole)
            {
                AllocConsole();
            }

            FormManager.showConnectForm();
            if (ConnectSuccess)
            {
                FormManager.showMainForm();
            }
        }
    }
}
