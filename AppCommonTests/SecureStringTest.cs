using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NUnit.Framework;
using passi_android.utils.Services.Certificate;

namespace AppCommonTests
{
    public class SecureStringTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CompareSecurestrings()
        {
            var str1 = new MySecureString("1111");
            var str2 = new MySecureString("1111");
            Assert.IsTrue(str1.Equals(str2));
        }
        [Test]
        public void CompareSecurestringsNotEqual()
        {
            var str1 = new MySecureString("1111");
            var str2 = new MySecureString("1211");
            Assert.IsFalse(str1.Equals(str2));
        }

        
    }
}