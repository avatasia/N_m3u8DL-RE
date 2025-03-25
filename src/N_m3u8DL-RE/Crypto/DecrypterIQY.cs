using N_m3u8DL_RE.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace N_m3u8DL_RE.Crypto
{
    internal class DecrypterIQY
    {
        private byte[] initXORKey;

        private byte[] encryptedData;

        private Aes ecpt;

        private DecrypterIQY()
        {
        }

        public DecrypterIQY(byte[] ticketKey, byte[] encryptedData)
        {
            this.encryptedData = encryptedData;
            ecpt = Aes.Create();
            ecpt.Key = ticketKey;
            ecpt.Mode = CipherMode.ECB;
            ecpt.Padding = PaddingMode.None;
            SetInitXORKey();
        }

        private void SetInitXORKey()
        {
            byte[] array = encryptedData.Skip(54).Take(32).ToArray();
            if (!array.All((byte x) => x == byte.MaxValue))
            {
                initXORKey = HexUtil.HexToBytes(Encoding.UTF8.GetString(array));
            }
        }

        private byte[] AES128Encrypt(byte[] data)
        {
            return ecpt.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);
        }

        public byte[] GetDecryptedData()
        {
            if (initXORKey == null)
            {
                return encryptedData;
            }
            byte[] array = encryptedData;
            byte[] destinationArray = new byte[array.Length];
            Array.Copy(array, destinationArray, array.Length);
            List<byte> list = new List<byte>();
            List<byte[]> list2 = new List<byte[]>();
            List<List<byte>> list3 = new List<List<byte>>();
            using (MemoryStream memoryStream = new MemoryStream(array))
            {
                int num = 0;
                int num2 = 0;
                byte[] array2 = new byte[188];
                while ((num = memoryStream.Read(array2, 0, array2.Length)) > 0)
                {
                    num2++;
                    IEnumerable<byte> source = array2.Take(num);
                    IEnumerable<byte> enumerable = source.Take(4);
                    uint num3 = BitConverter.ToUInt32(BitConverter.IsLittleEndian ? enumerable.Reverse().ToArray() : enumerable.ToArray(), 0);
                    uint num4 = (num3 & 0x1FFF00) >> 8;
                    uint num5 = (num3 & 0x30) >> 4;
                    uint num6 = (num3 & 0x400000) >> 22;
                    if (num4 != 256)
                    {
                        list3.Add(source.ToList());
                        continue;
                    }
                    List<byte> list4 = new List<byte>();
                    list4.AddRange(enumerable);
                    using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(source.Skip(4).ToArray())))
                    {
                        if (num5 == 3)
                        {
                            int num7 = binaryReader.ReadByte();
                            list4.Add((byte)num7);
                            list4.AddRange(binaryReader.ReadBytes(num7));
                        }
                        else
                        {
                            _ = 1;
                        }
                        if (num6 == 1)
                        {
                            if (list.Count > 3)
                            {
                                list2.Add(list.ToArray());
                                list.Clear();
                            }
                            list4.AddRange(binaryReader.ReadBytes(3));
                            list4.AddRange(binaryReader.ReadBytes(1));
                            byte[] array3 = binaryReader.ReadBytes(2);
                            list4.AddRange(array3);
                            if (BitConverter.ToInt16(BitConverter.IsLittleEndian ? array3.Reverse().ToArray() : array3, 0) == 0)
                            {
                                list4.AddRange(binaryReader.ReadBytes(2));
                                int num8 = binaryReader.ReadByte();
                                list4.Add((byte)num8);
                                list4.AddRange(binaryReader.ReadBytes(num8));
                            }
                        }
                        while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                        {
                            byte item = binaryReader.ReadByte();
                            list.Add(item);
                            if (list.Count > 5 && ((list[list.Count - 3] == 0 && list[list.Count - 2] == 0 && list[list.Count - 1] == 1) || (list[list.Count - 3] == 0 && list[list.Count - 2] == 0 && list[list.Count - 1] == 0)))
                            {
                                list2.Add(list.Take(list.Count - 3).ToArray());
                                list.Clear();
                                if (binaryReader.BaseStream.Position - 3 >= 0)
                                {
                                    binaryReader.BaseStream.Position -= 3L;
                                    continue;
                                }
                                list.AddRange(new byte[3] { 0, 0, 1 });
                            }
                        }
                    }
                    list3.Add(list4);
                }
                if (list.Count > 0)
                {
                    list2.Add(list.ToArray());
                    list.Clear();
                }
            }
            for (int i = 0; i < list2.Count; i++)
            {
                list2[i] = Decrypt(list2[i]);
            }
            using (BinaryReader binaryReader2 = new BinaryReader(new MemoryStream(list2.SelectMany((byte[] x) => x).ToArray())))
            {
                for (int j = 0; j < list3.Count; j++)
                {
                    if (list3[j].Count != 188 && list3[j].Count < 188)
                    {
                        int count = 188 - list3[j].Count;
                        list3[j].AddRange(binaryReader2.ReadBytes(count));
                    }
                }
            }
            return list3.SelectMany((List<byte> x) => x.ToArray()).ToArray();
        }

        public byte[] Decrypt(byte[] encrypted)
        {
            byte[] array = new byte[encrypted.Length];
            Array.Copy(encrypted, 0, array, 0, array.Length);
            List<byte> list = new List<byte>();
            using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(encrypted)))
            {
                while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                {
                    list.Add(binaryReader.ReadByte());
                    if (list.Count >= 4 && list[list.Count - 4] == 0 && list[list.Count - 3] == 0 && list[list.Count - 2] == 3 && (list[list.Count - 1] == 0 || list[list.Count - 1] == 1 || list[list.Count - 1] == 2 || list[list.Count - 1] == 3))
                    {
                        list.RemoveAt(list.Count - 2);
                        list.AddRange(binaryReader.ReadBytes(3));
                    }
                }
            }
            byte[] array2 = list.Skip(5).Take(list.Count - 5 - 2).ToArray();
            double num = Math.Ceiling((double)array2.Length / 16.0);
            int num2 = 0;
            for (int i = 1; (double)i <= num; i++)
            {
                byte[] bytes = BitConverter.GetBytes(i);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                byte[] array3 = initXORKey.Take(12).Concat(bytes).ToArray();
                if (i % 10 == 1 || (double)i == num)
                {
                    array3 = AES128Encrypt(array3);
                }
                byte[] array4 = array3;
                foreach (byte b in array4)
                {
                    array2[num2] ^= b;
                    num2++;
                    if (num2 == array2.Length)
                    {
                        break;
                    }
                }
            }
            Array.Copy(array2, 0, array, 5, array2.Length);
            return array;
        }
    }
}
