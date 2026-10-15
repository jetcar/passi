using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using passi_webapi.Controllers;
using Repos;
using Services;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WebApiDto.Certificate;
using WebApiDto.SignUp;

namespace PassiWebApiTests.Controllers
{
    public class CertificateControllerTests : TestBase
    {
        [Test]
        public void UpdatePublicCertRejectsAnUnknownParentCertThumbprintAsBadRequest()
        {
            var signupController = ServiceProvider.GetRequiredService<SignUpController>();
            var certificateController = ServiceProvider.GetRequiredService<CertificateController>();

            var email = $"User.{Guid.NewGuid()}@Passi.Cloud";
            var deviceId = Guid.NewGuid().ToString();
            ConfirmAccountOnDevice(signupController, email, Guid.NewGuid(), deviceId);

            var newCert = CreateCertificate(email);

            // ParentCertThumbprint does not match any stored certificate (typo, stale client,
            // already-rotated certificate). The controller must reject this as a client error
            // rather than dereferencing a null certificate record.
            var updateDto = new CertificateUpdateDto
            {
                PublicCert = Convert.ToBase64String(newCert.RawData),
                ParentCertThumbprint = "0000000000000000000000000000000000000000",
                ParentCertHashSignature = "unused",
                DeviceId = deviceId,
            };

            Assert.Throws<BadRequestException>(() => certificateController.UpdatePublicCert(updateDto));
        }

        private static X509Certificate2 ConfirmAccountOnDevice(SignUpController signupController, string email, Guid accountGuid, string deviceId)
        {
            signupController.SignUp(new SignupDto
            {
                Email = email,
                UserGuid = accountGuid,
                DeviceId = deviceId,
            });

            var cert = CreateCertificate(email);
            signupController.Confirm(new SignupConfirmationDto
            {
                Code = TestEmailSender.Code,
                DeviceId = deviceId,
                Email = email,
                Guid = accountGuid.ToString(),
                PublicCert = Convert.ToBase64String(cert.RawData),
            });

            return cert;
        }

        private static X509Certificate2 CreateCertificate(string email)
        {
            using var rsa = RSA.Create();
            var request = new CertificateRequest($"cn={email.Replace("@", "")}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        }
    }
}
