﻿using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MauiApp2.utils.Services.Certificate
{
    public class CertHelper : ICertHelper
    {
        private ISecureRepository _secureRepository;
        public CertHelper(ISecureRepository secureRepository)
        {
            _secureRepository = secureRepository;
        }

        public PublicCert ConvertToPublicCertificate(X509Certificate2 cert)
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

        public bool VerifyData(string data, string signedData, string base64PublicCert)
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

        public async Task<string> Sign(Guid accountGuid, MySecureString pin, string dataForSigning)
        {
            var account = _secureRepository.GetAccount(accountGuid);
            var privatecertificate = _secureRepository.GetCertificateWithKey(pin, account);
            return GetSignedData(dataForSigning, privatecertificate);
        }

        public string GetSignedData(string messageToSign, X509Certificate2 certificate)
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

        public async Task<string> SignByFingerPrint(string dataForSigning,
            X509Certificate2 privatecertificate)
        {
            return GetSignedData(dataForSigning, privatecertificate);
        }
    }

    public interface ICertHelper
    {
        PublicCert ConvertToPublicCertificate(X509Certificate2 cert);
        bool VerifyData(string data, string signedData, string base64PublicCert);
        Task<string> Sign(Guid accountGuid, MySecureString pin, string dataForSigning);
        string GetSignedData(string messageToSign, X509Certificate2 certificate);
        Task<string> SignByFingerPrint(string dataForSigning, X509Certificate2 privatecertificate);
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