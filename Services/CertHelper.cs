using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using GoogleTracer;
using log4net;

namespace Services;

[Profile]
public class CertHelper
{
    private static readonly ILog _logger = LogManager.GetLogger(typeof(CertHelper));

    // Test seam: lets tests observe that the certificate loaded here is disposed, without
    // changing VerifyData's public contract (callers still just pass the base64 cert string).
    internal static Func<byte[], X509Certificate2> LoadCertificate = X509CertificateLoader.LoadCertificate;

    public static bool VerifyData(string data, string signedData, string base64PublicCert)
    {
        if (data == null)
        {
            _logger.Error("VerifyData called with null data parameter");
            throw new ArgumentNullException(nameof(data));
        }
        if (signedData == null)
        {
            _logger.Error("VerifyData called with null signedData parameter");
            throw new ArgumentNullException(nameof(signedData));
        }
        if (base64PublicCert == null)
        {
            _logger.Error("VerifyData called with null base64PublicCert parameter");
            throw new ArgumentNullException(nameof(base64PublicCert));
        }

        _logger.Debug($"VerifyData called with data length: {data.Length}, signedData length: {signedData.Length}, cert length: {base64PublicCert.Length}");

        using var parentCert = LoadCertificate(Convert.FromBase64String(base64PublicCert));

        using (var sha512 = SHA512.Create())
        {
            // ComputeHash - returns byte array
            byte[] bytes = sha512.ComputeHash(Encoding.ASCII.GetBytes(data));

            using var rsaPublicKey = parentCert.GetRSAPublicKey();
            if (rsaPublicKey == null)
            {
                _logger.Error("VerifyData called with a certificate that has no RSA public key");
                throw new CryptographicException("Unsupported certificate key algorithm; RSA required");
            }

            var verify = rsaPublicKey.VerifyHash(bytes,
                Convert.FromBase64String(signedData), HashAlgorithmName.SHA512,
                RSASignaturePadding.Pkcs1);

            return verify;
        }
    }
}