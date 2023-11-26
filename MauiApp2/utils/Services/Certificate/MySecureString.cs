using System.Net;
using System.Runtime.InteropServices;
using System.Security;

namespace MauiApp2.utils.Services.Certificate
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
            return _string.IsEqualTo(secureString._string);

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
    public static class SecureStringExtensions
    {

        public static bool IsEqualTo(this SecureString ss1, SecureString ss2)
        {
            IntPtr bstr1 = IntPtr.Zero;
            IntPtr bstr2 = IntPtr.Zero;
            try
            {
                bstr1 = Marshal.SecureStringToBSTR(ss1);
                bstr2 = Marshal.SecureStringToBSTR(ss2);
                int length1 = Marshal.ReadInt32(bstr1, -4);
                int length2 = Marshal.ReadInt32(bstr2, -4);
                if (length1 == length2)
                {
                    for (int x = 0; x < length1; ++x)
                    {
                        byte b1 = Marshal.ReadByte(bstr1, x);
                        byte b2 = Marshal.ReadByte(bstr2, x);
                        if (b1 != b2) return false;
                    }
                }
                else return false;
                return true;
            }
            finally
            {
                if (bstr2 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr2);
                if (bstr1 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr1);
            }
        }
        public static SecureString AppendChars(this SecureString str, SecureString chars)
        {
            foreach (var charValue in chars.SecureStringToString())
            {
                str.AppendChar(charValue);
            }
            return str;
        }

        public static string SecureStringToString(this SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}