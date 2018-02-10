using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXEncryption
{
    public class ZXEncryptionException : Exception
    {
        public ZXEncryptionException() : base()
        {
        }


        public ZXEncryptionException(string message) : base(message)
        {
        }

        public ZXEncryptionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
