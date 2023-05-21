using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;

namespace passi_android.utils.Services.Certificate
{
    public class MySecureString
    {
        protected SecureString _string;

        public int Length { get { return _string.Length; } }

        public MySecureString(string str)
        {
            _string = new NetworkCredential("", str).SecurePassword;
        }
        public MySecureString(MySecureString str)
        {
            _string = new NetworkCredential("", str.SecureStringToString()).SecurePassword;
        }
        public MySecureString AppendChar(char str)
        {
            _string.AppendChar(str);
            return this;
        }
        public MySecureString Append(MySecureString str)
        {
            _string.AppendChars(str._string);
            return this;
        }
        public MySecureString TrimEnd(int length)
        {
            for (int i = 0; i < length; i++)
            {
                _string.RemoveAt(_string.Length - 1);
            }
            return this;
        }

        public string SecureStringToString()
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(_string);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        public override bool Equals(object obj)
        {
            var secureString = obj as MySecureString;
            return _string.Equals(secureString._string);

        }

        public void AppendChar(string value)
        {
            foreach (var c in value.ToCharArray())
            {
                _string.AppendChar(c);

            }
        }

        public string GetMasked(string s)
        {
            var result = "";
            for (int i = 0; i < _string.Length; i++)
            {
                result += s;
            }

            return result;
        }
    }
}