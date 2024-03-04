using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using passi_webapi.Controllers;
using Services;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WebApiDto.Certificate;
using WebApiDto.SignUp;

namespace PassiWebApiTests.Controllers
{
    public class SignupControllerTests : TestBase
    {
        [Test]
        public void SignUpTest()
        {
            var controller = ServiceProvider.GetService<SignUpController>();
            //act
            var signupDto = new SignupDto()
            {
                DeviceId = Guid.NewGuid().ToString(),
                Email = Guid.NewGuid().ToString() + "@passi.cloud",
                UserGuid = Guid.NewGuid()
            };
            controller.SignUp(signupDto);
            Assert.That(TestEmailSender.Code != null);
        }

        [Test]
        public void SignUpConfirmRenewTest()
        {
            var controller = ServiceProvider.GetService<SignUpController>();
            //act
            var signupDto = new SignupDto()
            {
                DeviceId = Guid.NewGuid().ToString(),
                Email = Guid.NewGuid().ToString() + "@passi.cloud",
                UserGuid = Guid.NewGuid()
            };
            controller.SignUp(signupDto);
            controller.Check(new SignupCheckDto()
            {
                Code = TestEmailSender.Code,
                Username = signupDto.Email
            });

            var rsa = RSA.Create(); // generate asymmetric key pair
            var distinguishedName = signupDto.Email;
            var req = new CertificateRequest($"cn={distinguishedName.Replace("@", "")}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
            Assert.That(cert.HasPrivateKey);

            var certificate64 = Convert.ToBase64String(cert.Export(X509ContentType.Pkcs12, "1234"));
            var certLoaded = new X509Certificate2(Convert.FromBase64String(certificate64), "1234", X509KeyStorageFlags.Exportable);
            Assert.That(certLoaded.HasPrivateKey);
            var signupConfirmationDto = new SignupConfirmationDto()
            {
                Code = TestEmailSender.Code,
                DeviceId = signupDto.DeviceId,
                Email = signupDto.Email,
                Guid = signupDto.UserGuid.ToString(),
                PublicCert = Convert.ToBase64String(certLoaded.RawData)
            };
            controller.Confirm(signupConfirmationDto);
            Assert.That(TestEmailSender.Code != null);
            //renew

            var rsa2 = RSA.Create(); // generate asymmetric key pair
            var distinguishedName2 = signupDto.Email;
            var req2 = new CertificateRequest($"cn={distinguishedName2.Replace("@", "")}", rsa2, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            var cert2 = req2.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

            var certController = ServiceProvider.GetService<CertificateController>();
            var parentCertHashSignature = GetSignedData(Convert.ToBase64String(cert2.RawData), cert);
            var newCert64 = Convert.ToBase64String(cert2.RawData);
            var valid = CertHelper.VerifyData(newCert64, parentCertHashSignature, signupConfirmationDto.PublicCert);
            Assert.That(valid);
            var certificateUpdateDto = new CertificateUpdateDto()
            {
                DeviceId = signupDto.DeviceId,
                ParentCertThumbprint = certLoaded.Thumbprint,
                PublicCert = newCert64,
                ParentCertHashSignature = parentCertHashSignature
            };
            var updateResponse = certController.UpdatePublicCert(certificateUpdateDto);
            var x509Certificate2 = new X509Certificate2(Convert.FromBase64String(updateResponse.PublicCert));
            Assert.That(cert2.Thumbprint.Equals(x509Certificate2.Thumbprint));
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

        [Test]
        public void SignUpAndConfirmInvalidCodeTest()
        {
            RandomGenerator generator = new RandomGenerator();
            var controller = ServiceProvider.GetService<SignUpController>();
            //act
            var signupDto = new SignupDto()
            {
                DeviceId = Guid.NewGuid().ToString(),
                Email = Guid.NewGuid().ToString() + "@passi.cloud",
                UserGuid = Guid.NewGuid()
            };
            controller.SignUp(signupDto);

            var ecdsa = RSA.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"cn={signupDto.Email}", ecdsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var dateTime = DateTime.UtcNow.AddDays(-1);

            var cert = req.CreateSelfSigned(dateTime, dateTime.AddYears(1));

            var certificate = Convert.ToBase64String(cert.GetRawCertData());

            var signupConfirmationDto = new SignupConfirmationDto()
            {
                Code = generator.GetNumbersString(6),
                DeviceId = signupDto.DeviceId,
                Email = signupDto.Email,
                Guid = signupDto.UserGuid.ToString(),
                PublicCert = certificate
            };
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    controller = ServiceProvider.GetService<SignUpController>();
                    controller.Confirm(signupConfirmationDto);
                }
                catch (Exception e)
                {
                }
            }

            signupConfirmationDto.Code = TestEmailSender.Code;
            controller = ServiceProvider.GetService<SignUpController>();
            var exception = Assert.Throws<BadRequestException>(() => controller.Confirm(signupConfirmationDto));
            Assert.That(exception.Message == "Code not found");
        }
    }
}