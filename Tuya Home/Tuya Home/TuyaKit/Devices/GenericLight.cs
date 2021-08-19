using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tuya_Home.Kit.Devices
{


	[Serializable]
	public class GenericLight : Device
	{
		public void setColourForm()
        {
			System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
			colorDialog.AllowFullOpen = true;
			colorDialog.AnyColor = true;
			colorDialog.SolidColorOnly = false;

			if (colorDialog.ShowDialog() == DialogResult.OK)
            {
				setColour(colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
            }
        }

		public async void setColour(int R, int G, int B)
		{
			Dictionary<string, object> response;
			if (R == 255 && G == 255 && B == 255)
			{
				response = await Set(
				new Dictionary<string, object>
				{
					["1"] = true,
					["2"] = "white",
				});
			}
			else
			{
				response = await Set(
				   new Dictionary<string, object>
				   {
					   ["1"] = true,
					   ["2"] = "colour",
					   ["5"] = R.ToString("X2").ToLower() + G.ToString("X2").ToLower() + B.ToString("X2").ToLower() + "0000ffff",
				   });
			}

			Log.Format("response: `{0}`", response);
		}

		public async void setBrightness(int brightness)
		{
			Dictionary<string, object> response = await Set(
				new Dictionary<string, object>
				{
					["1"] = true,
					["3"] = brightness
				}
			);

			Log.Format("response: `{0}`", response);
		}
	}
}