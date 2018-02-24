using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace ZXEncryption
{
    /// <summary>
    /// Zx encryption class
    /// </summary>
    [ComVisible(true)]
    [Guid("7D652ED3-24DC-44F0-BFB9-9FB08B549FC2")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("ZXCrypt.ZXCryptor")]
    public class ZXCryptor : IZXCryptor
    {
        public void EncryptFile(string inputFile, string outputFile, byte[] passcode)
        {
            try
            {
                EtmCryptography crypt = new EtmCryptography();
                crypt.EncryptFile(inputFile, outputFile, passcode);
            }
            catch (Exception ex)
            {
                throw new ZXEncryptionException("Encryption error occurred.", ex);
            }
        }

        public void DecryptFile(string inputFile, string outputFile, byte[] passcode)
        {
            try
            {
                EtmCryptography crypt = new EtmCryptography();
                crypt.DecryptFile(inputFile, outputFile, passcode);
            }
            catch (Exception ex)
            {
                throw new ZXEncryptionException("Encryption error occurred.", ex);
            }
        }

        public bool CompareFiles(string fileA, string fileB)
        {
            if (!File.Exists(fileA) || !File.Exists(fileB))
                return false;

            try
            {
                using (FileStream fsa = new FileStream(fileA, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream fsb = new FileStream(fileB, FileMode.Open, FileAccess.Read))
                    {
                        // compare file size
                        if (fsa.Length != fsb.Length)
                            return false;

                        int bufferSize = 1024 * sizeof(Int64);
                        byte[] bufferA = new byte[bufferSize];
                        byte[] bufferB = new byte[bufferSize];

                        while (true)
                        {
                            int bytesReadA = fsa.Read(bufferA, 0, bufferSize);
                            int bytesReadB = fsb.Read(bufferB, 0, bufferSize);

                            if (bytesReadA != bytesReadB)
                                return false;
                            if (bytesReadA == 0)
                                return true;

                            // compare byte to byte
                            if (!CompareArrays(bufferA, bufferB, bytesReadA))
                                return false;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public byte[] EncryptBytes(byte[] bytesToBeEncrypted, byte[] passcode)
        {
            if (bytesToBeEncrypted == null || bytesToBeEncrypted.Length == 0)
                return null;

            try
            {
                AesCryptography aesCryptor = new AesCryptography();
                byte[] encryptedBytes = aesCryptor.EncryptBytes(bytesToBeEncrypted, passcode);
                return encryptedBytes;
            }
            catch (Exception ex)
            {
                throw new ZXEncryptionException("Encryption error occurred.", ex);
            }
        }

        public byte[] DecryptBytes(byte[] bytesToBeDecrypted, byte[] passcode)
        {
            if (bytesToBeDecrypted == null || bytesToBeDecrypted.Length == 0)
                return null;

            try
            {
                AesCryptography aesCryptor = new AesCryptography();
                byte[] decryptedBytes = aesCryptor.DecryptBytes(bytesToBeDecrypted, passcode);
                return decryptedBytes;
            }
            catch (Exception ex)
            {
                throw new ZXEncryptionException("Encryption error occurred.", ex);
            }
        }

        public string EncryptText(string input, byte[] passcode)
        {
            if (String.IsNullOrWhiteSpace(input))
                return String.Empty;

            try
            {
                byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);

                AesCryptography aesCryptor = new AesCryptography();
                byte[] encryptedBytes = aesCryptor.EncryptBytes(bytesToBeEncrypted, passcode);
                string output = Convert.ToBase64String(encryptedBytes);
                return output;
            }
            catch (ZXEncryptionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ZXEncryptionException("Encryption error occurred.", ex);
            }
        }

        public string DecryptText(string input, byte[] passcode)
        {
            if (String.IsNullOrWhiteSpace(input))
                return String.Empty;

            try
            {
                byte[] bytesToBeDecrypted = Convert.FromBase64String(input);

                AesCryptography aesCryptor = new AesCryptography();
                byte[] decryptedBytes = aesCryptor.DecryptBytes(bytesToBeDecrypted, passcode);
                string output = Encoding.UTF8.GetString(decryptedBytes);
                return output;
            }
            catch (ZXEncryptionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ZXEncryptionException("Encryption error occurred.", ex);
            }
        }

        #region Private functions

        private bool CompareArrays(byte[] arrayA, byte[] arrayB, int bytesToCompare = 0)
        {
            if (arrayA.Length != arrayB.Length)
                return false;

            int length = (bytesToCompare == 0) ? arrayA.Length : bytesToCompare;
            int tailIdx = length - length % sizeof(Int64);

            //check in 8 byte chunks
            for (int i = 0; i < tailIdx; i += sizeof(Int64))
            {
                if (BitConverter.ToInt64(arrayA, i) != BitConverter.ToInt64(arrayB, i))
                    return false;
            }

            //check the remainder of the array, always shorter than 8 bytes
            for (var i = tailIdx; i < length; i++)
            {
                if (arrayA[i] != arrayB[i])
                    return false;
            }

            return true;
        }

        #endregion
    }
}
