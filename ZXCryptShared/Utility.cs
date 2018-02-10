using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXCryptShared
{
    public static class Utility
    {
        public static string ByteArrayToHexString(byte[] arr)
        {
            string str = BitConverter.ToString(arr);
            return str.Replace("-", String.Empty);
        }

        public static byte[] HexStringToByteArray(string str)
        {
            byte[] arr = new byte[str.Length / 2];
            for (int i = 0; i < str.Length; i += 2)
            {
                arr[i / 2] = Convert.ToByte(str.Substring(i, 2), 16);
            }

            return arr;
        }
    }
}
