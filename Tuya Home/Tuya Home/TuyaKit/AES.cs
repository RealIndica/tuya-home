using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace Tuya_Home.TuyaKit
{
    public static class AES
    {
        public static string AES_encrypt(string Input, string AES_Key, string AES_IV)
        {
            // Create encryptor
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 128;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Convert.FromBase64String(AES_Key);
            aes.IV = Convert.FromBase64String(AES_IV);
            ICryptoTransform encrypt = aes.CreateEncryptor(aes.Key, aes.IV);

            // Encrypt Input
            byte[] xBuff = null;
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert from UTF-8 String to byte array, write to memory stream and encrypt, then convert to byte array
                using (CryptoStream cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write))
                {
                    byte[] xXml = Encoding.UTF8.GetBytes(Input);
                    cs.Write(xXml, 0, xXml.Length);
                }
                xBuff = ms.ToArray();
            }

            // Convert from byte array to base64 string then return
            string Output = Convert.ToBase64String(xBuff);
            return Output;
        }
    }
}
