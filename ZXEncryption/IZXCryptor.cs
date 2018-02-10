using System;
using System.Runtime.InteropServices;

namespace ZXEncryption
{
    /// <summary>
    /// Interface for encryption methods
    /// </summary>
    [ComVisible(true)]
    [Guid("80F3BFA9-1707-400C-B59C-92FD608D1D2C")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IZXCryptor
    {
        /// <summary>
        /// Encrypt a file
        /// </summary>
        /// <param name="inputFile">full path of the file to be encrypted</param>
        /// <param name="outputFile">full path of the encrypted file</param>
        /// <param name="passcode">password to encrypt the file</param>
        void EncryptFile(string inputFile, string outputFile, byte[] passcode);

        /// <summary>
        /// Decrypt a file
        /// </summary>
        /// <param name="inputFile">full path of the file to be decrypted</param>
        /// <param name="outputFile">full path of the decrypted file</param>
        /// <param name="passcode">password to decrypt the file</param>
        void DecryptFile(string inputFile, string outputFile, byte[] passcode);

        /// <summary>
        /// Binary comparison of two files
        /// </summary>
        /// <param name="fileA">full path of the first file to compare</param>
        /// <param name="fileB">full path of the second file to compare</param>
        /// <returns>true if the content of the two files are exactly the same</returns>
        bool CompareFiles(string fileA, string fileB);

        /// <summary>
        /// Encrypt an array of bytes
        /// </summary>
        /// <param name="bytesToBeEncrypted">Bytes to be encrypted</param>
        /// <param name="passcode">password to encrypt the byte array</param>
        /// <returns>Encrypted byte array</returns>
        byte[] EncryptBytes(byte[] bytesToBeEncrypted, byte[] passcode);

        /// <summary>
        /// Decrypt an array of bytes
        /// </summary>
        /// <param name="bytesToBeDecrypted">Bytes to be decrypted</param>
        /// <param name="passcode">password to decrypt the byte array</param>
        /// <returns>Decrypted byte array</returns>
        byte[] DecryptBytes(byte[] bytesToBeDecrypted, byte[] passcode);

        /// <summary>
        /// Encrypt a string
        /// </summary>
        /// <param name="input">String to be encrypted</param>
        /// <param name="passcode">password to encrypt the string</param>
        /// <returns>Encrypted string</returns>
        string EncryptText(string input, byte[] passcode);

        /// <summary>
        /// Decrypt a string
        /// </summary>
        /// <param name="input">String to b decrypted</param>
        /// <param name="passcode">password to decrypt the string</param>
        /// <returns>Decrypted string</returns>
        string DecryptText(string input, byte[] passcode);
    }
}
