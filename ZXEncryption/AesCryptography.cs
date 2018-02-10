using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace ZXEncryption
{
    /// <summary>
    /// Cryptography class which uses AES to encrypt and decrypt files/bytes/text
    /// </summary>
    public class AesCryptography
    {
        private const int _passwordIterations = 30000;
        private const int _minSaltSize = 16;
        private const int _maxSaltSize = 64;
        private const int _currentVersionNumber = 1;
        private const int _versionNumberLength = 4;

        private const string _versionAES256 = "AES2";
        private const string _currentVersion = _versionAES256;

        #region Encryption methods

        /// <summary>
        /// Encrypt a file
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        /// <param name="passcode">password to encrypt the file</param>
        public void EncryptFile(string inputFile, string outputFile, byte[] passcode)
        {
            CryptFile(inputFile, outputFile, passcode, true);
        }

        /// <summary>
        /// Decrypt a file
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        /// <param name="passcode">password to decrypt the file</param>
        public void DecryptFile(string inputFile, string outputFile, byte[] passcode)
        {
            CryptFile(inputFile, outputFile, passcode, false);
        }

        /// <summary>
        /// Encrypt an array of bytes
        /// </summary>
        /// <param name="bytesToBeEncrypted">Bytes to be encrypted</param>
        /// <param name="passcode">password to encrypt the byte array</param>
        /// <returns>Encrypted byte array</returns>
        public byte[] EncryptBytes(byte[] bytesToBeEncrypted, byte[] passcode)
        {
            byte[] outputBytes = CryptBytes(bytesToBeEncrypted, passcode, true);
            return outputBytes;
        }

        /// <summary>
        /// Decrypt an array of bytes
        /// </summary>
        /// <param name="bytesToBeDecrypted">Bytes to be decrypted</param>
        /// <param name="passcode">password to decrypt the byte array</param>
        /// <returns>Decrypted byte array</returns>
        public byte[] DecryptBytes(byte[] bytesToBeDecrypted, byte[] passcode)
        {
            byte[] outputBytes = CryptBytes(bytesToBeDecrypted, passcode, false);
            return outputBytes;
        }

        #endregion

        #region Private helper functions

        /// <summary>
        /// Encrypt or decrypt file
        /// </summary>
        /// <param name="inputfile">input file</param>
        /// <param name="outputfile">output file</param>
        /// <param name="passcode">password to encrypt or decrypt the file</param>
        /// <param name="encrypt">true: encrypt; false: decrypt</param>
        private void CryptFile(string inputfile, string outputfile, byte[] passcode, bool encrypt)
        {
            string outputDir = Path.GetDirectoryName(outputfile);
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            // remove output readonly flag
            if (File.Exists(outputfile))
            {
                FileInfo fi = new FileInfo(outputfile);
                fi.IsReadOnly = false;
            }

            // Create input and output file streams.
            using (FileStream inputStream = new FileStream(inputfile, FileMode.Open, FileAccess.Read))
            {
                using (FileStream outputStream = new FileStream(outputfile, FileMode.Create, FileAccess.Write))
                {
                    CryptStream(inputStream, outputStream, passcode, encrypt);
                }
            }
        }

        /// <summary>
        /// Encrypt or decrypt input stream into output stream
        /// </summary>
        /// <param name="inputStream">input stream</param>
        /// <param name="outputStream">output stream</param>
        /// <param name="passcode">password to encrypt or decrypt the file</param>
        /// <param name="encrypt">true: encrypt; false: decrypt</param>
        private void CryptStream(Stream inputStream, Stream outputStream, byte[] passcode, bool encrypt)
        {
            long inputFileSize = inputStream.Length;

            byte[] key = null;
            byte[] iv = null;
            byte[] saltSizeBytes = new byte[4];

            using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
            {
                if (aesProvider.KeySize < 256)
                    aesProvider.KeySize = 256;

                // Make the encryptor or decryptor.
                ICryptoTransform cryptor;
                if (encrypt)
                {
                    byte[] salt = GenerateRandomSalt(_minSaltSize, _maxSaltSize);

                    // Generate the key and initialization vector.
                    GenerateKeyAndIV(passcode, salt, aesProvider.KeySize, aesProvider.BlockSize, out key, out iv);

                    cryptor = aesProvider.CreateEncryptor(key, iv);

                    // add version to the output file
                    WriteVersionNumber(outputStream, _currentVersion);

                    // Add salt size to the start of the output file
                    saltSizeBytes[0] = (byte)(salt.Length >> 24);
                    saltSizeBytes[1] = (byte)(salt.Length >> 16);
                    saltSizeBytes[2] = (byte)(salt.Length >> 8);
                    saltSizeBytes[3] = (byte)salt.Length;
                    outputStream.Write(saltSizeBytes, 0, 4);

                    // Add salt to the output file
                    outputStream.Write(salt, 0, salt.Length);
                }
                else
                {
                    // read and validate version
                    ReadAndValidateKeyVersion(inputStream);

                    // Read salt size from input file
                    if (inputStream.Read(saltSizeBytes, 0, 4) < 4)
                        throw new ArgumentException("Failed to read salt size from the input file");

                    int saltSize = (saltSizeBytes[0] << 24) | (saltSizeBytes[1] << 16) | (saltSizeBytes[2] << 8) | (saltSizeBytes[3]);

                    // Read salt from input file
                    byte[] salt = new byte[saltSize];
                    if (inputStream.Read(salt, 0, saltSize) < saltSize)
                        throw new ArgumentException("Failed to read salt from the input file");

                    // Generate the key and initialization vector.
                    GenerateKeyAndIV(passcode, salt, aesProvider.KeySize, aesProvider.BlockSize, out key, out iv);

                    cryptor = aesProvider.CreateDecryptor(key, iv);
                }

                try
                {
                    using (CryptoStream cryptoStream = new CryptoStream(outputStream, cryptor, CryptoStreamMode.Write))
                    {
                        long fileSizeMedium = 10 * 1024 * 1024;
                        long fileSizeLarge = 200 * 1024 * 1024;

                        // different buffer size based on input file size
                        int bufferSize = 64 * 1024;
                        if (inputFileSize > fileSizeMedium && inputFileSize <= fileSizeLarge)
                            bufferSize = 2 * 1024 * 1024;
                        else if (inputFileSize > fileSizeLarge)
                            bufferSize = 4 * 1024 * 1024;

                        byte[] buffer = new byte[bufferSize];

                        while (true)
                        {
                            // Read bytes.
                            int bytesRead = inputStream.Read(buffer, 0, bufferSize);
                            if (bytesRead == 0)
                                break;

                            // Write the bytes into the CryptoStream.
                            cryptoStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
                finally
                {
                    if (cryptor != null)
                        cryptor.Dispose();
                }
            }       // using AesCryptoServiceProvider
        }

        /// <summary>
        /// Encrypt or decrypt an input byte array
        /// </summary>
        /// <param name="inputBytes">byte array to be encrypted or decrypted</param>
        /// <param name="encrypt">true: encrypt; false: decrypt</param>
        /// <returns>Encrypted or decrypted byte array</returns>
        private byte[] CryptBytes(byte[] inputBytes, byte[] passcode, bool encrypt)
        {
            byte[] outputBytes = null;
            byte[] key = null;
            byte[] iv = null;

            int saltSize = 16;

            using (MemoryStream ms = new MemoryStream())
            {
                using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
                {
                    if (aesProvider.KeySize < 256)
                        aesProvider.KeySize = 256;

                    if (encrypt)
                    {
                        // 16-byte random salt
                        byte[] salt = GenerateRandomSalt(saltSize, saltSize);

                        GenerateKeyAndIV(passcode, salt, aesProvider.KeySize, aesProvider.BlockSize, out key, out iv);

                        using (ICryptoTransform cryptor = aesProvider.CreateEncryptor(key, iv))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(ms, cryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(inputBytes, 0, inputBytes.Length);
                                cryptoStream.Close();

                                // put the salt in the begin of the output byte array
                                outputBytes = salt.Concat(ms.ToArray()).ToArray();
                            }
                        }
                    }
                    else
                    {
                        // get salt from input byte array
                        byte[] salt = new byte[saltSize];
                        Array.Copy(inputBytes, salt, saltSize);

                        GenerateKeyAndIV(passcode, salt, aesProvider.KeySize, aesProvider.BlockSize, out key, out iv);

                        using (ICryptoTransform cryptor = aesProvider.CreateDecryptor(key, iv))
                        {
                            int byteSize = inputBytes.Length - saltSize;
                            if (byteSize > 0)
                            {
                                byte[] encryptedBytes = new byte[byteSize];
                                Array.Copy(inputBytes, saltSize, encryptedBytes, 0, byteSize);

                                using (CryptoStream cryptoStream = new CryptoStream(ms, cryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(encryptedBytes, 0, inputBytes.Length);
                                    cryptoStream.Close();
                                    outputBytes = ms.ToArray();
                                }
                            }
                        }
                    }
                }
            }

            return outputBytes;
        }

        /// <summary>
        /// Generate key and IV with key phrase and salt
        /// </summary>
        /// <param name="keyPhrase"></param>
        /// <param name="salt"></param>
        /// <param name="keySize"></param>
        /// <param name="blockSize"></param>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        private void GenerateKeyAndIV(byte[] keyPhrase, byte[] salt, int keySize, int blockSize, out byte[] key, out byte[] iv)
        {
            //Rfc2898DeriveBytes takes a password and salt to generate keys with PBKDF2 (Password Based Key Derivation Function #2)
            Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(keyPhrase, salt, _passwordIterations);

            // GetBytes() returns a different key on each invocation
            key = deriveBytes.GetBytes(keySize / 8);
            iv = deriveBytes.GetBytes(blockSize / 8);
        }

        /// <summary>
        /// Generate a random salt
        /// </summary>
        /// <param name="minSize">minumum size of the salt</param>
        /// <param name="maxSize">maximum size of the salt</param>
        /// <returns></returns>
        private byte[] GenerateRandomSalt(int minSize, int maxSize)
        {
            if (minSize > maxSize || minSize <= 0 || maxSize <= 0)
                throw new ArgumentOutOfRangeException();

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                // 1. generate salt size
                int saltSize = minSize;
                if (minSize != maxSize)
                {
                    byte[] randomBytes = new byte[4];
                    rng.GetBytes(randomBytes);
                    int rand = ((randomBytes[0] & 0x7f) << 24) | (randomBytes[1] << 16) | (randomBytes[2] << 8) | (randomBytes[3]);
                    saltSize = minSize + (rand % (maxSize - minSize + 1));
                }

                // 2. generate salt
                byte[] salt = new byte[saltSize];
                rng.GetNonZeroBytes(salt);

                return salt;
            }
        }

        private void ReadAndValidateKeyVersion(Stream protectedKeyStream)
        {
            byte[] keyFileVersion = new byte[_versionNumberLength];
            protectedKeyStream.Read(keyFileVersion, 0, keyFileVersion.Length);

            string ver = System.Text.Encoding.ASCII.GetString(keyFileVersion);
            if (!Equals(ver, _currentVersion))
            {
                //throw new InvalidOperationException("File versions do not match with the current algorithm version");
            }
        }

        private void WriteVersionNumber(Stream outputStream, string verNum)
        {
            byte[] keyFileVersion = System.Text.Encoding.ASCII.GetBytes(verNum);
            if (keyFileVersion.Length != _versionNumberLength)
            {
                throw new InvalidOperationException("Invalid algorithm version");
            }
            outputStream.Write(keyFileVersion, 0, keyFileVersion.Length);
        }

        #endregion
    }
}
