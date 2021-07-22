using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tuya_Home
{
    public static class FormManager
    {
        private static void closeAllForms()
        {
            try
            {
                for (int i = 0; i < Application.OpenForms.Count; i++)
                {
                    Application.OpenForms[i].Close();
                }
            }
            catch (Exception) { }
        }

        public static void showConnectForm()
        {
            closeAllForms();
            Application.Run(new Connect());
        }

        public static void showMainForm()
        {
            closeAllForms();
            Application.Run(new Main());
        }
    }
}
