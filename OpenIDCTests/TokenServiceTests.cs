using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ConfigurationManager;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using OpenIDC.Services;

namespace OpenIDCTests;

public class TokenServiceTests
{
    [TearDown]
    public void ResetSeam()
    {
        TokenService.ExtractRsaPublicKey = certificate => certificate.GetRSAPublicKey();
    }

    [Test]
    public void GetJsonWebKeySetDisposesTheRsaPublicKeyExtractedFromAnX509SigningCertificate()
    {
        using var cert = CreateCertificate();
        var trackingRsa = new DisposeTrackingRsa(cert.GetRSAPublicKey());
        TokenService.ExtractRsaPublicKey = _ => trackingRsa;

        var signingCredentials = new SigningCredentials(new X509SecurityKey(cert), SecurityAlgorithms.RsaSha256);
        var tokenService = new TokenService(CreateAppSetting(), signingCredentials);

        tokenService.GetJsonWebKeySet();

        // GetJsonWebKeySet extracts an RSA public key from the signing certificate on every call
        // (this backs the public /.well-known/jwks endpoint); nothing else owns that RSA instance,
        // so GetJsonWebKeySet itself must dispose it instead of leaking its native key handle.
        Assert.That(trackingRsa.WasDisposed, Is.True);
    }

    private static AppSetting CreateAppSetting()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
        var appSetting = new AppSetting(configuration);
        appSetting["IdentityUrlBase"] = "https://passi.cloud";
        return appSetting;
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"cn={Guid.NewGuid()}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }

    // Forwards just enough of RSA (the abstract members) to a real key so GetJsonWebKeySet's
    // ExportParameters(false) call works, while letting the test observe whether it was disposed.
    private class DisposeTrackingRsa : RSA
    {
        private readonly RSA _inner;
        public bool WasDisposed { get; private set; }

        public DisposeTrackingRsa(RSA inner)
        {
            _inner = inner;
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters) =>
            _inner.ExportParameters(includePrivateParameters);

        public override void ImportParameters(RSAParameters parameters) =>
            _inner.ImportParameters(parameters);

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
