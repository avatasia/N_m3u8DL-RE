using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace N_m3u8DL_RE.Crypto
{
    internal class DecrypterYK
    {
        private static byte SYNC_BYTE = 71;

        private static int TS_PACKET_LENGTH = 188;

        private static byte[] Combine(params byte[][] arrays)
        {
            byte[] array = new byte[arrays.Sum((byte[] a) => a.Length)];
            int num = 0;
            foreach (byte[] array2 in arrays)
            {
                Array.Copy(array2, 0, array, num, array2.Length);
                num += array2.Length;
            }
            return array;
        }

        public static List<byte[]> SplitByteArray(byte[] bytes, int BlockLength)
        {
            List<byte[]> list = new List<byte[]>();
            for (int i = 0; i < bytes.Length; i += BlockLength)
            {
                byte[] array;
                if (i + BlockLength > bytes.Length)
                {
                    array = new byte[bytes.Length - i];
                    Buffer.BlockCopy(bytes, i, array, 0, bytes.Length - i);
                }
                else
                {
                    array = new byte[BlockLength];
                    Buffer.BlockCopy(bytes, i, array, 0, BlockLength);
                }
                list.Add(array);
            }
            return list;
        }

        private static byte[] DecryptAES(ICryptoTransform cryptoTransform, byte[] encryptData)
        {
            int inputCount = encryptData.Length - encryptData.Length % 16;
            byte[] array = cryptoTransform.TransformFinalBlock(encryptData, 0, inputCount);
            Array.Copy(array, encryptData, array.Length);
            return encryptData;
        }

        private static int GetPacketStartOffset(byte[] raw)
        {
            if (raw.Length == TS_PACKET_LENGTH)
            {
                return 0;
            }
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] == SYNC_BYTE && raw[i + TS_PACKET_LENGTH] == SYNC_BYTE)
                {
                    return i;
                }
            }
            return -1;
        }

        public static byte[] Decrypt(byte[] encrtpyData, byte[] keybuffer, byte[] ivByte)
        {
            long num = 0L;
            MemoryStream memoryStream = new MemoryStream();
            Aes aes = Aes.Create();
            aes.BlockSize = 128;
            aes.KeySize = 128;
            aes.Key = keybuffer;
            aes.IV = ivByte;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            ICryptoTransform cryptoTransform = aes.CreateDecryptor();
            MemoryStream memoryStream2 = new MemoryStream();
            int packetStartOffset = GetPacketStartOffset(encrtpyData);
            MemoryStream memoryStream3 = new MemoryStream(encrtpyData.Skip(packetStartOffset - 1).ToArray());
            long length = memoryStream3.Length;
            List<byte[]> list = new List<byte[]>();
            bool flag = true;
            int num2 = -1;
            while (num < length)
            {
                byte[] array = new byte[188];
                int num3 = memoryStream3.Read(array, 0, TS_PACKET_LENGTH);
                num += num3;
                if (array[0] != 71)
                {
                    continue;
                }
                bool flag2 = Convert.ToBoolean(0x40 & array[1]);
                int num4 = ((0x1F & array[1]) << 8) | array[2];
                int num5 = 4;
                if (1 < (0x30 & array[3]) >> 4)
                {
                    num5 += array[num5] + 1;
                }
                if (num4 == 0)
                {
                    num2 = GetPMTPid(array.Skip(num5 + 1).ToArray());
                }
                if ((num4 >= 0 && num4 <= 31) || num4 == num2)
                {
                    memoryStream2.Write(array, 0, array.Length);
                    continue;
                }
                byte[] item = array.Take(num5).ToArray();
                byte[] array2 = array.Skip(num5).ToArray();
                if (flag)
                {
                    flag2 = false;
                    flag = false;
                }
                list.Add(item);
                if (!flag2)
                {
                    memoryStream.Write(array2, 0, array2.Length);
                    continue;
                }
                byte[] array3 = memoryStream.ToArray();
                int count = array3[8] + 9;
                byte[] encryptData = array3.Skip(count).ToArray();
                byte[] array4 = DecryptAES(cryptoTransform, encryptData);
                MemoryStream memoryStream4 = new MemoryStream(Combine(array3.Take(count).ToArray(), array4));
                for (int i = 0; i < list.Count - 1; i++)
                {
                    memoryStream2.Write(list[i], 0, list[i].Length);
                    int num6 = TS_PACKET_LENGTH - list[i].Length;
                    byte[] array5 = new byte[num6];
                    memoryStream4.Read(array5, 0, num6);
                    memoryStream2.Write(array5, 0, array5.Length);
                }
                list.RemoveRange(0, list.Count - 1);
                memoryStream = new MemoryStream();
                memoryStream.Write(array2, 0, array2.Length);
            }
            if (memoryStream.ToArray().Length != 0)
            {
                byte[] array6 = memoryStream.ToArray();
                int count2 = array6[8] + 9;
                byte[] encryptData2 = array6.Skip(count2).ToArray();
                byte[] array7 = DecryptAES(cryptoTransform, encryptData2);
                MemoryStream memoryStream5 = new MemoryStream(Combine(array6.Take(count2).ToArray(), array7));
                for (int j = 0; j < list.Count; j++)
                {
                    memoryStream2.Write(list[j], 0, list[j].Length);
                    int num7 = TS_PACKET_LENGTH - list[j].Length;
                    byte[] array8 = new byte[num7];
                    memoryStream5.Read(array8, 0, num7);
                    memoryStream2.Write(array8, 0, array8.Length);
                }
            }
            memoryStream2.Close();
            return memoryStream2.ToArray();
        }

        private static int GetPMTPid(byte[] nowPacket)
        {
            return ((0x1F & nowPacket[10]) << 8) | nowPacket[11];
        }
    }
}
