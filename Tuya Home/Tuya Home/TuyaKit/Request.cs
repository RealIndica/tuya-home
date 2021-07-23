//
// Copyright (c) 2017 Geri Borbás http://www.twitter.com/_eppz
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Tuya_Home.Kit
{


    public class Request
    {


        // Packet protocol from https://github.com/codetheweb/tuyapi/wiki/Packet-Structure
        byte[] prefixBytes = new byte[] { 0x00, 0x00, 0x55, 0xaa };
        byte[] versionBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        byte[] commandBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        byte[] payloadLengthBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        byte[] spacingBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        byte[] ver33Bytes = new byte[] { 0x33, 0x2e, 0x33 };

        // Payload (data, checksum, suffix).
        byte[] checksumBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        byte[] suffixBytes = new byte[] { 0x00, 0x00, 0xaa, 0x55 };

        // Command values from https://github.com/codetheweb/tuyapi/wiki/TUYA-Commands
        public enum Command
        {
            SetStatus = 0x07,
            GetStatus = 0x0a,
            GetSSIDList = 0x0b
        }


        #region Features

        public async Task<JObject> SendJSONObjectForCommandToDevice(object JSON, Command command, Device device, bool encrypt = true)
        { return await SendJSONStringForCommandToDevice(JsonConvert.SerializeObject(JSON, Formatting.None), command, device, encrypt); }

        public async Task<JObject> SendJSONStringForCommandToDevice(string JSON, Command command, Device device, bool encrypt)
        {
            Log.Format("Request.SendJSONStringForCommandToDevice, JSON: \r\n`{0}`", JSON);

            // Request.
            byte[] dataBytes = (encrypt) ? EncryptedBytesFromJSONForDevice(JSON, device) : Encoding.UTF8.GetBytes(JSON);
            byte[] packetBytes = PacketFromDataForCommand(dataBytes, command, device);

            // Send.
            byte[] responsePacketBytes = await SendPacketToDevice(packetBytes, device);

            // Validate.
            if (IsValidPacket(responsePacketBytes) == false)
            { return null; }
            Log.Format("Validated Packet Response");

            // Parse.
            string responseJSONString = DataStringFromPacket(responsePacketBytes);
            Log.Format("responseJSONString: `{0}`", responseJSONString);

            // Only if any.
            if (responseJSONString == string.Empty)
                return new JObject();

            // Create object.
            JObject responseJSONObject = JObject.Parse(responseJSONString);

            return responseJSONObject;
        }

        #endregion


        #region Communication

        public async Task<byte[]> SendPacketToDevice(byte[] packetBytes, Device device)
        {
            Log.Format("Request.SendDataToDevice(), packetBytes.Length: `{0}`", packetBytes.Length);

            using (TcpClient tcpClient = new TcpClient(device.IP, device.port))
            using (NetworkStream networkStream = tcpClient.GetStream())
            using (MemoryStream responseMemoryStream = new MemoryStream())
            {
                // Write request.
                await networkStream.WriteAsync(packetBytes, 0, packetBytes.Length);

                // Read response.
                byte[] responseBytes = new byte[1024];
                int numberOfBytesResponded = await networkStream.ReadAsync(responseBytes, 0, responseBytes.Length);
                responseMemoryStream.Write(responseBytes, 0, numberOfBytesResponded);

                // Close client.
                networkStream.Close();
                tcpClient.Close();

                // Return byte array.
                return responseMemoryStream.ToArray();
            }
        }

        #endregion


        #region Packet assembly

        bool IsValidPacket(byte[] packetBytes)
        {
            // Emptyness.
            if (packetBytes == null)
            {
                Log.Format("Empty packet.");
                return false;
            }

            // Lengths.
            int headerLength = prefixBytes.Length + versionBytes.Length + commandBytes.Length + payloadLengthBytes.Length + spacingBytes.Length + ver33Bytes.Length;
            int minimumPayloadLength = checksumBytes.Length + suffixBytes.Length;
            int minimumPacketLength = headerLength + minimumPayloadLength;

            // Length.
            if (packetBytes.Length < minimumPacketLength)
            {
                Log.Format("Invalid packet length.");
                return false;
            }

            // Prefix.
            if (packetBytes.Take(4).SequenceEqual(prefixBytes) == false)
            {
                Log.Format("Invalid prefix.");
                return false;
            }

            // Suffix.
            if (packetBytes.Skip(packetBytes.Length - 4).Take(4).SequenceEqual(suffixBytes) == false)
            {
                Log.Format("Invalid suffix.");
                return false;
            }

            // Payload.
            int payloadLength = BitConverter.ToInt32(packetBytes.Skip(prefixBytes.Length + versionBytes.Length + commandBytes.Length + spacingBytes.Length + ver33Bytes.Length).Take(payloadLengthBytes.Length).Reverse().ToArray(), 0);
            if (packetBytes.Length < headerLength + payloadLength)
            {
                Log.Format("Missing payload.");
                return false;
            }

            // Valid.
            return true;
        }

        protected string DataStringFromPacket(byte[] packetBytes)
        {
            // Lengths.
            int headerLength = prefixBytes.Length + versionBytes.Length + commandBytes.Length + payloadLengthBytes.Length + spacingBytes.Length + ver33Bytes.Length;
            int suffixLength = checksumBytes.Length + suffixBytes.Length;

            // Data.
            byte[] packetPayloadBytes = packetBytes.Skip(headerLength).ToArray(); // Skip header
            byte[] packetDataBytes = packetPayloadBytes.Take(packetPayloadBytes.Length - suffixLength).ToArray(); // Trim suffix

            // To string.
            byte[] packetDataBytesWithoutLeadingZeroes = packetDataBytes.SkipWhile((byte eachByte, int eachIndex) => eachByte == 0x00).ToArray();
            string packetDataString = Encoding.UTF8.GetString(packetDataBytesWithoutLeadingZeroes);

            return packetDataString;
        }

        public static void PrintByteArray(byte[] ba, string name)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
                hex.Append("-");
            }

            Console.WriteLine("\r\n[" + name + "] [" + ba.Length.ToString() + "]\r\n" + hex.ToString());
        }

        private byte[] nullBytes(int count)
        {
            List<byte> b = new List<byte>();
            for (int i = 0; i < count; i++)
            {
                b.Add(0x00);
            }
            return b.ToArray();
        }

        private byte[] SubArray(byte[] array, int startIndex, int endIndex)
        {
            byte[] result = new byte[endIndex - startIndex];
            Array.Copy(array, startIndex, result, 0, endIndex - startIndex);
            return result;
        }

        private byte[] AddArray(params byte[][] arrays)
        {
            List<byte> tempArray = new List<byte>();
            foreach (byte b in arrays.SelectMany(x => x))
            {
                tempArray.Add(b);
            }
            return tempArray.ToArray();
        }

        private int ByteSearch(byte[] src, byte[] pattern)
        {
            int maxFirstCharSlot = src.Length - pattern.Length + 1;
            for (int i = 0; i < maxFirstCharSlot; i++)
            {
                if (src[i] != pattern[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = pattern.Length - 1; j >= 1; j--)
                {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;
        }

        protected byte[] PacketFromDataForCommand(byte[] dataBytes, Command command, Device dev)
        {
            // Set command.
            commandBytes[3] = (byte)command;

            // Count payload length
            int payloadLengthInt = ver33Bytes.Length + spacingBytes.Length + dataBytes.Length + checksumBytes.Length + suffixBytes.Length;
            payloadLengthBytes = BitConverter.GetBytes(payloadLengthInt);
            if (BitConverter.IsLittleEndian) Array.Reverse(payloadLengthBytes); // Big endian

            // Assemble packet.
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Header (prefix, version, command, payload length).
                memoryStream.Write(prefixBytes, 0, prefixBytes.Length); //4
                PrintByteArray(prefixBytes, "Prefix Bytes"); 

                memoryStream.Write(versionBytes, 0, versionBytes.Length); 
                PrintByteArray(versionBytes, "Version Bytes");

                memoryStream.Write(commandBytes, 0, commandBytes.Length); 
                PrintByteArray(commandBytes, "Command Bytes");

                memoryStream.Write(payloadLengthBytes, 0, payloadLengthBytes.Length); 
                PrintByteArray(payloadLengthBytes, "Payload Length Bytes");

                // 3.3 Pre-payload
                if (command == Command.SetStatus)
                {
                    memoryStream.Write(ver33Bytes, 0, ver33Bytes.Length); 
                    PrintByteArray(ver33Bytes, "Ver33 Bytes");
                }

                // Space between header and payload
                memoryStream.Write(spacingBytes, 0, spacingBytes.Length); 
                PrintByteArray(spacingBytes, "Spacing Bytes");

                // Payload (data, checksum, suffix).
                memoryStream.Write(dataBytes, 0, dataBytes.Length);
                PrintByteArray(dataBytes, "Data Bytes");


                byte[] checksumTarget = memoryStream.ToArray();
                PrintByteArray(checksumTarget, "Checksum Target");

                uint crc = Crc32.crc32(checksumTarget) & 0xFFFFFFFF;
                checksumBytes = Crc32.intToBytes(crc);
                memoryStream.Write(checksumBytes, 0, checksumBytes.Length);
                PrintByteArray(checksumBytes, "Checksum Bytes");


                memoryStream.Write(suffixBytes, 0, suffixBytes.Length);
                PrintByteArray(suffixBytes, "Suffix Bytes");


                return memoryStream.ToArray();
            }
        }


        #endregion


        #region Encryption

        // From https://github.com/codetheweb/tuyapi/blob/master/index.js#L300
        byte[] EncryptedBytesFromJSONForDevice(string JSON, Device device)
        {
            Log.Format("Request.EncryptedBytesFromJSONForDevice()");

            // Key.
            byte[] xkey = Encoding.UTF8.GetBytes(device.localKey);

            // Encrypt with key.
            using (var aes = new AesManaged() { Mode = CipherMode.ECB, Key = xkey, Padding = PaddingMode.PKCS7  })
            {
                byte[] encryptedData;

                using (var input = new MemoryStream(Encoding.UTF8.GetBytes(JSON)))
                using (var output = new MemoryStream())
                {
                    ICryptoTransform encryptor = aes.CreateEncryptor();
                    using (var cryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
                    {
                        var buffer = new byte[1024];
                        var read = input.Read(buffer, 0, buffer.Length);
                        while (read > 0)
                        {
                            cryptStream.Write(buffer, 0, read);
                            read = input.Read(buffer, 0, buffer.Length);
                        }
                        cryptStream.FlushFinalBlock();
                        encryptedData = output.ToArray();
                    }
                }

                return encryptedData;
            }

        }

        #endregion

        #region temp
        byte[] xEncryptedBytesFromJSONForDevice(string JSON, Device device)
        {
            Log.Format("Request.EncryptedBytesFromJSONForDevice()");

            // Key.
            byte[] key = Encoding.UTF8.GetBytes(device.localKey);

            // Encrypt with key.
            string encryptedJSONBase64String;
            using (AesManaged aes = new AesManaged() { Mode = CipherMode.ECB, Key = key })
            using (MemoryStream encryptedStream = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(encryptedStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                byte[] JSONBytes = Encoding.UTF8.GetBytes(JSON);
                cryptoStream.Write(JSONBytes, 0, JSONBytes.Length);
                StreamReader reader = new StreamReader(encryptedStream);
                string text = reader.ReadToEnd();
                cryptoStream.Close();
                //encryptedJSONBase64String = Convert.ToBase64String(encryptedStream.ToArray());
                encryptedJSONBase64String = text;
            }

            // Create hash.
            string hashString;
            using (MD5 md5 = MD5.Create())
            using (MemoryStream hashBaseStream = new MemoryStream())
            {
                byte[] encryptedPayload = Encoding.UTF8.GetBytes($"data={encryptedJSONBase64String}||lpv={device.protocolVersion}||");
                hashBaseStream.Write(encryptedPayload, 0, encryptedPayload.Length);
                hashBaseStream.Write(key, 0, key.Length);
                byte[] hashBytes = md5.ComputeHash(hashBaseStream.ToArray());
                string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
                hashString = hash.Substring(8, 16);
            }

            // Stitch together.
            return Encoding.UTF8.GetBytes($"{device.protocolVersion}{hashString}{encryptedJSONBase64String}");
        }

        /*protected byte[] PacketFromDataForCommand(byte[] dataBytes, Command command, Device dev)
{
    // Assemble packet.
    using (MemoryStream memoryStream = new MemoryStream())
    {
        using (BinaryWriter bw = new BinaryWriter(memoryStream))
        {
            memoryStream.Write(nullBytes(1024), 0, 1024);

            memoryStream.Position = 0;
            memoryStream.Write(prefixBytes, 0, prefixBytes.Length);

            int len = prefixBytes.Length - 1;

            memoryStream.Position = 8 + len;
            memoryStream.Write(new byte[] { (byte)command }, 0, 1);

            memoryStream.Position = 12 + len;
            bw.Write((int)dataBytes.Length + 8);

            memoryStream.Position = 13 + len;
            byte[] vBytes = Encoding.UTF8.GetBytes($"{dev.protocolVersion}");
            memoryStream.Write(vBytes, 0, vBytes.Length);

            byte[] selected = SubArray(memoryStream.ToArray(), 0, dataBytes.Length + 16);
            UInt32 crc = Crc32.crc32(selected) & 0xFFFFFFFF;

            memoryStream.Position = dataBytes.Length + 16;
            memoryStream.Write(BitConverter.GetBytes(crc), 0, BitConverter.GetBytes(crc).Length);

            memoryStream.Write(suffixBytes, 0, suffixBytes.Length);

            int delIdx = ByteSearch(memoryStream.ToArray(), suffixBytes);
            byte[] fin = SubArray(memoryStream.ToArray(), 0, delIdx + suffixBytes.Length);
            PrintByteArray(BitConverter.GetBytes(crc).);
            return fin;
        }
    }
}*/
        #endregion
    }
}