using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace ZXCryptShared
{
    /// <summary>
    /// Wrapper class for SecureString
    /// </summary>
    /// <see cref="http://web.archive.org/web/20090928112609/http://dotnet.org.za/markn/archive/2008/10/04/handling-passwords.aspx" />
    public class SecureStringWrapper : IDisposable
    {
        private SecureString _secureText;
        private string _clearText;
        private GCHandle _gchText;
        private byte[] _byteArray;
        private GCHandle _gchByte;

        public SecureStringWrapper()
        {
        }

        public SecureStringWrapper(SecureString secureString)
        {
            SecureText = secureString;
        }

        public SecureString SecureText
        {
            get { return _secureText; }
            set
            {
                _secureText = value;
                Update();
            }
        }

        public void Dispose()
        {
            Deallocate();
        }

        public string ClearText
        {
            get { return _clearText; }
            protected set { _clearText = value; }
        }

        public byte[] ByteArray
        {
            get { return _byteArray; }
            set { _byteArray = value; }
        }

        private unsafe void Update()
        {
            Deallocate();

            if (SecureText != null)
            {
                int length = SecureText.Length;
                ClearText = new string('\0', length);
                ByteArray = new byte[length];

                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    _gchText = GCHandle.Alloc(ClearText, GCHandleType.Pinned);
                    _gchByte = GCHandle.Alloc(ByteArray, GCHandleType.Pinned);
                }

                IntPtr intPtrText = IntPtr.Zero;
                IntPtr intPtrByte = IntPtr.Zero;
                RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(
                    delegate
                    {
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try { }
                        finally
                        {
                            intPtrText = Marshal.SecureStringToBSTR(SecureText);
                            intPtrByte = Marshal.SecureStringToGlobalAllocAnsi(SecureText);
                        }

                        char* pStr = (char*)intPtrText;
                        char* pClearText = (char*)_gchText.AddrOfPinnedObject();
                        for (int index = 0; index < length; index++)
                        {
                            pClearText[index] = pStr[index];
                        }

                        byte* pByte = (byte*)intPtrByte;
                        byte* pByteArr = (byte*)_gchByte.AddrOfPinnedObject();
                        for (int index = 0; index < length; index++)
                        {
                            pByteArr[index] = pByte[index];
                        }
                    },
                    delegate
                    {
                        if (intPtrText != IntPtr.Zero)
                        {
                            Marshal.ZeroFreeBSTR(intPtrText);
                        }
                        if (intPtrByte != IntPtr.Zero)
                        {
                            Marshal.ZeroFreeGlobalAllocAnsi(intPtrByte);
                        }
                    },
                    null);
            }
        }

        private unsafe void Deallocate()
        {
            if (_gchText.IsAllocated)
            {
                int length = ClearText.Length;
                char* pClearText = (char*)_gchText.AddrOfPinnedObject();
                for (int index = 0; index < length; index++)
                {
                    pClearText[index] = '\0';
                }

                _gchText.Free();
            }

            if (_gchByte.IsAllocated)
            {
                int length = ByteArray.Length;
                byte* pByteArr = (byte*)_gchByte.AddrOfPinnedObject();
                for (int index = 0; index < length; index++)
                {
                    pByteArr[index] = 0;
                }

                _gchByte.Free();
            }
        }

    }
}
