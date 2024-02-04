using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using passi_webapi.Controllers;
using Services;
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
        public void SignUpAndConfirmTest()
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
                Email = signupDto.Email
            });

            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"cn={signupDto.Email}", ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

            var certificate = Convert.ToBase64String(cert.GetRawCertData());

            controller.Confirm(new SignupConfirmationDto()
            {
                Code = TestEmailSender.Code,
                DeviceId = signupDto.DeviceId,
                Email = signupDto.Email,
                Guid = signupDto.UserGuid.ToString(),
                PublicCert = certificate
            });
            Assert.That(TestEmailSender.Code != null);
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

            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"cn={signupDto.Email}", ecdsa, HashAlgorithmName.SHA256);
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