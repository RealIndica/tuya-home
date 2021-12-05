using System;


namespace Tuya_Home.Kit
{


    public class Log
    {


        public static void Format(string formatString, params object[] parameters)
        {

#if UNITY_EDITOR
            UnityEngine.Debug.LogFormat(formatString, parameters);
#else
            Console.WriteLine(string.Format(formatString, parameters));
#endif

        }
    }
}