﻿using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MauiViewModels.utils.Services.Certificate;

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

            var rsaPrivateKey = certificate.GetRSAPrivateKey();
            var signedBytes = rsaPrivateKey
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

    Task<string> Sign(Guid accountGuid, MySecureString pin, string dataForSigning);

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