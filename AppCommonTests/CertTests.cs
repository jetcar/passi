using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AppCommon.Certificate;
using NUnit.Framework;

namespace AppCommonTests
{
    public class CertTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void SignByCertAndVerify()
        {
            var task = Certificates.GenerateCertificate("test@mail.ee", "1234").Result;
            string password = task.Item2;
            byte[] cert = task.Item3;
            var certificate = new X509Certificate2(cert, password + "1234", X509KeyStorageFlags.Exportable);

            var data = "111111111111111111111111111111111111111";
            var signedData = CertHelper.GetSignedData(data, certificate);

            var publicCertBytes = certificate.GetRawCertData();
            var publicCert = new X509Certificate2(publicCertBytes);

            using (var sha512 = SHA512.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha512.ComputeHash(Encoding.ASCII.GetBytes(data));

                var rsaPublicKey = publicCert.GetRSAPublicKey();
                var verify = rsaPublicKey.VerifyHash(bytes,
                    Convert.FromBase64String(signedData), HashAlgorithmName.SHA512,
                    RSASignaturePadding.Pkcs1);

                Assert.IsTrue(verify);
            }
        }

        [Test]
        public void SignByCertAndVerify2()
        {
            var task = Certificates.GenerateCertificate("test@mail.ee", "1234").Result;
            string password = task.Item2;
            byte[] cert = task.Item3;
            var certificate = new X509Certificate2(cert, password + "1234", X509KeyStorageFlags.Exportable);

            var data = "111111111111111111111111111111111111111";
            var signedData = CertHelper.GetSignedData(data, certificate);

            var publicCertBytes = certificate.GetRawCertData();
            var publicCert = new X509Certificate2(publicCertBytes);
            var verify = CertHelper.VerifyData(data, signedData, Convert.ToBase64String(publicCertBytes));

            Assert.IsTrue(verify);
        }

        [Test]
        public void ExportImportVerify()
        {
            var task = Certificates.GenerateCertificate("test@mail.ee", "1234").Result;
            string password = task.Item2;
            byte[] cert = task.Item3;
            var certificate = new X509Certificate2(cert, password + "1234", X509KeyStorageFlags.Exportable);

            var certificateBytesFingerPrint = Convert.ToBase64String(certificate.Export(X509ContentType.Pkcs12, "4321"));
            var certificate2 =
                new X509Certificate2(Convert.FromBase64String(certificateBytesFingerPrint), "4321", X509KeyStorageFlags.Exportable);
            var data = "111111111111111111111111111111111111111";
            var signedData = CertHelper.GetSignedData(data, certificate2);

            var publicCertBytes = certificate.GetRawCertData();
            var publicCert = new X509Certificate2(publicCertBytes);
            var verify = CertHelper.VerifyData(data, signedData, Convert.ToBase64String(publicCertBytes));

            Assert.IsTrue(verify);
        }

        [Test]
        public void GetPrivateKeyFail()
        {
            var task = Certificates.GenerateCertificate("test@mail.ee", "1234").Result;
            string password = task.Item2;
            byte[] cert = task.Item3;
            var certificate = task.Item1;

            var base64String = Convert.ToBase64String(certificate.GetRawCertData());
            var fromBase64String = Convert.FromBase64String(base64String);
            var publicCert = new X509Certificate(fromBase64String);
        }
    }
}