using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using GoogleTracer;

namespace Services;

[Profile]
public class CertHelper
{
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
}