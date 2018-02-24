using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace ZXEncryption
{
    /// <summary>
    /// Authenticated Encryption class using Encrypt-then-MAC (AES-then-MAC) method. 
    /// </summary>
    /// <remarks>
    /// EtM uses 2 separate keys – encryption key Ke and MAC key Km
    /// EtM encryption process is fairly simple:
    ///     1. AES-encrypt plaintext P with a secret key Ke and a freshly-generated random IV to obtain ciphertext C1.
    ///     2. Append the ciphertext C1 to the used IV to obtain C2 = IV + C1.
    ///     3. Calculate MAC = HMAC(Km, C2) where Km is a different secret key independent of Ke.
    ///     4. Append MAC to C2 and return C3 = C2 + MAC.
    /// EtM decryption process is also simple:
    ///     1. If input C length is less than(expected HMAC length + expected AES IV length), abort.
    ///     2. Read MACexpected = last - HMAC - size bytes of C.
    ///     3. Calculate MACactual = HMAC(Km, C - without - last - HMAC - size bytes).
    ///     4. If BAC(MACexpected , MACactual) is false, abort.
    ///     5. Set IV = (take - IV - size bytes from start of C). Set C2 = (bytes of C between IV and MAC).
    ///     6. AES-decrypt C2 with IV and Ke to obtain plaintext P.Return P.
    /// </remarks>
    /// <see cref="http://securitydriven.net/"/>
    /// <see cref="https://gist.github.com/jbtule/4336842#file-aesthenhmac-cs"/>
    public class EtmCryptography
    {
        private const int _passwordIterations = 30000;
        private const int _aesKeyBitSize = 256;
        private const int _aesBlockBitSize = 128;

        private const int _minSaltSize = 16;
        private const int _maxSaltSize = 32;
        private const int _currentVersionNumber = 1;
        private const int _versionNumberLength = 4;
        private const long _maxHashSize = 50 * 1024 * 1024;

        private const string _versionEtm = "EtM1";
        private const string _currentVersion = _versionEtm;

        private const long _fileSizeLarge = 100 * 1024 * 1024;

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
                using (FileStream outputStream = new FileStream(outputfile, FileMode.Create, FileAccess.ReadWrite))
                {
                    if (encrypt)
                    {
                        EncryptStream(inputStream, outputStream, passcode);
                    }
                    else
                    {
                        DecryptStream(inputStream, outputStream, passcode);
                    }
                }
            }
        }

        /// <summary>
        /// Encrypt file stream
        /// </summary>
        /// <param name="inputStream">input stream</param>
        /// <param name="outputStream">output stream</param>
        /// <param name="passcode">password to encrypt or decrypt the file</param>
        private void EncryptStream(FileStream inputStream, FileStream outputStream, byte[] passcode)
        {
            if (passcode == null || passcode.Length == 0)
                throw new ArgumentException("Passcode is empty", "passcode");

            long inputFileSize = inputStream.Length;
            if (inputFileSize <= 0)
                throw new ArgumentException("Input file is empty", "inputStream");

            // Overall 2MB buffer performs better than any other buffer size (even better than the 50MB/100MB). However it requires 
            // higher CPU usage than any others. For small files, use 64KB. For large files, use 2MB
            int bufferSize = (inputFileSize < _fileSizeLarge) ? 64 * 1024 : 2 * 1024 * 1024;
            byte[] buffer = new byte[bufferSize];

            // random salts and keys for encryption and MAC
            byte[] saltSizeBytes = new byte[4];
            byte[] encSalt = GenerateRandomSalt(_minSaltSize, _maxSaltSize);
            byte[] encKey = GenerateKey(passcode, encSalt, _aesKeyBitSize);
            byte[] macSalt = GenerateRandomSalt(_minSaltSize, _maxSaltSize);
            byte[] macKey = GenerateKey(passcode, macSalt, _aesKeyBitSize);

            // 1. version (4 bytes)
            WriteVersionNumber(outputStream, _currentVersion);

            // 2. size of encSalt (4 bytes)
            saltSizeBytes[0] = (byte)(encSalt.Length >> 24);
            saltSizeBytes[1] = (byte)(encSalt.Length >> 16);
            saltSizeBytes[2] = (byte)(encSalt.Length >> 8);
            saltSizeBytes[3] = (byte)encSalt.Length;
            outputStream.Write(saltSizeBytes, 0, 4);

            // 3. encSalt
            outputStream.Write(encSalt, 0, encSalt.Length);

            // 4. size of macSalt (4 bytes)
            saltSizeBytes[0] = (byte)(macSalt.Length >> 24);
            saltSizeBytes[1] = (byte)(macSalt.Length >> 16);
            saltSizeBytes[2] = (byte)(macSalt.Length >> 8);
            saltSizeBytes[3] = (byte)macSalt.Length;
            outputStream.Write(saltSizeBytes, 0, 4);

            // 5. macSalt
            outputStream.Write(macSalt, 0, macSalt.Length);

            using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider
            {
                KeySize = _aesKeyBitSize,
                BlockSize = _aesBlockBitSize
            })
            {
                // 6. IV (should be aesProvider.BlockSize/8 bytes)
                aesProvider.GenerateIV();
                byte[] iv = aesProvider.IV;
                outputStream.Write(iv, 0, iv.Length);

                int bytesRead = 0;
                using (var cryptor = aesProvider.CreateEncryptor(encKey, iv))
                {
                    // 7. encrypt file
                    using (var cryptoStream = new CryptoStream(outputStream, cryptor, CryptoStreamMode.Write))
                    {
                        while (true)
                        {
                            // Read bytes.
                            bytesRead = inputStream.Read(buffer, 0, bufferSize);
                            if (bytesRead == 0)
                                break;

                            // Write the bytes into the CryptoStream.
                            cryptoStream.Write(buffer, 0, bytesRead);
                        }

                        cryptoStream.FlushFinalBlock();

                        // hash output file. If file is too big, only hash the first 50MB
                        bufferSize = (int)Math.Min(_maxHashSize, outputStream.Length);
                        buffer = new byte[bufferSize];
                        outputStream.Seek(0, SeekOrigin.Begin);
                        bytesRead = outputStream.Read(buffer, 0, bufferSize);
                        using (var hmac = new HMACSHA512(macKey))
                        {
                            var mac = hmac.ComputeHash(buffer, 0, bytesRead);

                            // 8. append hash at the end of file
                            outputStream.Seek(0, SeekOrigin.End);
                            outputStream.Write(mac, 0, hmac.HashSize / 8);
                        }
                    }
                }
            }       // using AesCryptoServiceProvider
        }

        /// <summary>
        /// Encrypt or decrypt input stream into output stream
        /// </summary>
        /// <param name="inputStream">input stream</param>
        /// <param name="outputStream">output stream</param>
        /// <param name="passcode">password to encrypt or decrypt the file</param>
        private void DecryptStream(FileStream inputStream, FileStream outputStream, byte[] passcode)
        {
            if (passcode == null || passcode.Length == 0)
                throw new ArgumentException("Passcode is empty", "passcode");

            long inputFileSize = inputStream.Length;
            if (inputFileSize < (_versionNumberLength + 4 + _minSaltSize + 4 + _minSaltSize))
                throw new ArgumentException("Invalid input file size", "inputStream");

            // 1. read and validate version
            ReadAndValidateKeyVersion(inputStream);

            // 2. Read size of encSalt
            byte[] saltSizeBytes = new byte[4];
            if (inputStream.Read(saltSizeBytes, 0, 4) < 4)
                throw new ArgumentException("Failed to read encSalt size from the input file");

            // 3. read encSalt
            int saltSize = (saltSizeBytes[0] << 24) | (saltSizeBytes[1] << 16) | (saltSizeBytes[2] << 8) | (saltSizeBytes[3]);
            byte[] encSalt = new byte[saltSize];
            if (inputStream.Read(encSalt, 0, saltSize) < saltSize)
                throw new ArgumentException("Failed to read endSalt from the input file");

            // 4. read size of macSalt
            if (inputStream.Read(saltSizeBytes, 0, 4) < 4)
                throw new ArgumentException("Failed to read macSalt size from the input file");

            // 5. read macSalt
            saltSize = (saltSizeBytes[0] << 24) | (saltSizeBytes[1] << 16) | (saltSizeBytes[2] << 8) | (saltSizeBytes[3]);
            byte[] macSalt = new byte[saltSize];
            if (inputStream.Read(macSalt, 0, saltSize) < saltSize)
                throw new ArgumentException("Failed to read macSalt from the input file");

            // Generate the keys
            byte[] encKey = GenerateKey(passcode, encSalt, _aesKeyBitSize);
            byte[] macKey = GenerateKey(passcode, macSalt, _aesKeyBitSize);

            // authenticate with MAC
            long headerLength = _versionNumberLength + 4 + encSalt.Length + 4 + macSalt.Length;
            long ivLength = _aesBlockBitSize / 8;
            using (var hmac = new HMACSHA512(macKey))
            {
                long hashLength = hmac.HashSize / 8;

                if (inputFileSize <= (headerLength + ivLength + hashLength))
                    throw new ArgumentException("Invalid input file size");

                int bufSize = (int)Math.Min(_maxHashSize, inputFileSize - hashLength);
                var hashBuf = new byte[bufSize];
                inputStream.Seek(0, SeekOrigin.Begin);
                if (inputStream.Read(hashBuf, 0, bufSize) < bufSize)
                    throw new ArgumentException("Invalid input file content");
                var computedMac = hmac.ComputeHash(hashBuf, 0, bufSize);

                var actualMac = new byte[hashLength];
                inputStream.Seek(inputFileSize - hashLength, SeekOrigin.Begin);
                inputStream.Read(actualMac, 0, (int)hashLength);

                if (!XorCompare(computedMac, actualMac))
                    throw new ArgumentException("Invalid input file");

                // reset stream position
                inputStream.Seek(headerLength, SeekOrigin.Begin);

                // 6. read IV
                var iv = new byte[ivLength];
                if (inputStream.Read(iv, 0, (int)ivLength) < ivLength)
                    throw new ArgumentException("Failed to read IV from the input file");

                // 7. decrypt file
                long actualFileSize = inputFileSize - headerLength - ivLength - hashLength;

                // Overall 2MB buffer performs better than any other buffer size (even better than the 50MB/100MB). However it requires 
                // higher CPU usage than any others. For small files, use 64KB. For large files, use 2MB
                int bufferSize = (inputFileSize < _fileSizeLarge) ? 64 * 1024 : 2 * 1024 * 1024;
                byte[] buffer = new byte[bufferSize];

                using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider
                {
                    KeySize = _aesKeyBitSize,
                    BlockSize = _aesBlockBitSize
                })
                {
                    using (var cryptor = aesProvider.CreateDecryptor(encKey, iv))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(outputStream, cryptor, CryptoStreamMode.Write))
                        {
                            long bytesToRead = actualFileSize;
                            while (bytesToRead > 0)
                            {
                                int count = (bytesToRead > (long)bufferSize) ? bufferSize : (int)bytesToRead;
                                int bytesRead = inputStream.Read(buffer, 0, count);
                                if (bytesRead == 0)
                                    break;

                                cryptoStream.Write(buffer, 0, bytesRead);
                                bytesToRead -= bytesRead;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generate key based on passcode and salt
        /// </summary>
        /// <param name="passcode"></param>
        /// <param name="salt"></param>
        /// <param name="keySize"></param>
        /// <returns></returns>
        private byte[] GenerateKey(byte[] passcode, byte[] salt, int keySize)
        {
            //Rfc2898DeriveBytes takes a password and salt to generate keys with PBKDF2 (Password Based Key Derivation Function #2)
            Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(passcode, salt, _passwordIterations);

            // GetBytes() returns a different key on each invocation
            byte[] key = deriveBytes.GetBytes(keySize / 8);
            return key;
        }

        /// <summary>
        /// Generate a random salt
        /// </summary>
        /// <param name="minSize"></param>
        /// <param name="maxSize"></param>
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

        private  bool XorCompare(byte[] a, byte[] b)
        {
            var x = a.Length ^ b.Length;
            for (var i = 0; i < a.Length; ++i)
            {
                x |= a[i] ^ b[i % b.Length];
            }
            return x == 0;
        }

        #endregion

    }
}
