using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace passi_android.utils.Certificate
{
    public static class CertHelper
    {
        public static X509Certificate2 GetCertificateWithKey(Guid accountGuid, string pin)
        {
            var account = SecureRepository.GetAccount(accountGuid);
            return new X509Certificate2(Convert.FromBase64String(account.PrivateCertBinary), account.Password + pin, X509KeyStorageFlags.Exportable);
        }

        public static PublicCert ConvertToPublicCertificate(X509Certificate2 cert)
        {
            var publicCertJson = new PublicCert()
            {
                BinaryData = Convert.ToBase64String(cert.GetRawCertData()),
                Email = cert.GetNameInfo(X509NameType.SimpleName, true),
                NotAfter = cert.NotAfter,
                NotBefore = cert.NotBefore,
                Thumbprint = cert.Thumbprint
            };
            return publicCertJson;
        }

        public static bool VerifyData(string data, string signedData, string base64PublicCert)
        {
            var parentCert = new X509Certificate2(Convert.FromBase64String(base64PublicCert));

            using (var sha512 = SHA512.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha512.ComputeHash(Encoding.ASCII.GetBytes(data));

                var verify = parentCert.GetRSAPublicKey().VerifyHash(bytes,
                    Convert.FromBase64String(signedData), HashAlgorithmName.SHA512,
                    RSASignaturePadding.Pkcs1);

                return verify;
            }
        }

        public static async Task<string> Sign(Guid accountGuid, string pin, string dataForSigning)
        {
            var privatecertificate = GetCertificateWithKey(accountGuid, pin);
            return GetSignedData(dataForSigning, privatecertificate);
        }

        public static string GetSignedData(string messageToSign, X509Certificate2 certificate)
        {
            using (var sha512 = SHA512.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(messageToSign));

                var signedBytes = certificate.GetRSAPrivateKey()
                    .SignHash(bytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

                return Convert.ToBase64String(signedBytes);
            }
        }

       
        public static async Task<string> SignByFingerPrint(Guid accountGuid, string dataForSigning)
        {
            var privatecertificate = SecureRepository.GetCertificateWithFingerPrint(accountGuid);
            return GetSignedData(dataForSigning, privatecertificate);
        }
    }

    public class PublicCert
    {
        public string Email { get; set; }
        public string BinaryData { get; set; }
        public string Thumbprint { get; set; }
        public DateTime? NotBefore { get; set; }
        public DateTime? NotAfter { get; set; }
    }
}