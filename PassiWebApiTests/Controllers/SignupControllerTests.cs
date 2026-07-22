using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using passi_webapi.Controllers;
using Repos;
using Services;
using System;
using System.Linq;
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
            // The confirmation email is dispatched on a background Task, so TestEmailSender.Code is
            // racy; assert on the synchronously-persisted invitation code instead.
            Assert.That(GetLatestCode(signupDto.Email), Is.Not.Null);
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
            var code = GetLatestCode(signupDto.Email);
            controller.Check(new SignupCheckDto()
            {
                Code = code,
                Username = signupDto.Email
            });

            var rsa = RSA.Create(); // generate asymmetric key pair
            var distinguishedName = signupDto.Email;
            var req = new CertificateRequest($"cn={distinguishedName.Replace("@", "")}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
            Assert.That(cert.HasPrivateKey);

            var certificate64 = Convert.ToBase64String(cert.Export(X509ContentType.Pkcs12, "1234"));
            var certLoaded = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(certificate64), "1234", X509KeyStorageFlags.Exportable);
            Assert.That(certLoaded.HasPrivateKey);
            var signupConfirmationDto = new SignupConfirmationDto()
            {
                Code = code,
                DeviceId = signupDto.DeviceId,
                Email = signupDto.Email,
                Guid = signupDto.UserGuid.ToString(),
                PublicCert = Convert.ToBase64String(certLoaded.RawData)
            };
            controller.Confirm(signupConfirmationDto);
            Assert.That(code, Is.Not.Null);
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
            var x509Certificate2 = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(updateResponse.PublicCert));
            Assert.That(cert2.Thumbprint.Equals(x509Certificate2.Thumbprint));
        }

        [Test]
        public void ConfirmReturnsCanonicalAccountGuidWhenExistingUserReenrolls()
        {
            var email = Guid.NewGuid() + "@passi.cloud";
            var deviceId = Guid.NewGuid().ToString();
            var originalGuid = Guid.NewGuid();

            // First enrollment (new user) adopts the client-provided GUID.
            var controller = ServiceProvider.GetService<SignUpController>();
            controller.SignUp(new SignupDto { DeviceId = deviceId, Email = email, UserGuid = originalGuid });
            var firstGuid = ConfirmAndGetAccountGuid(controller, email, deviceId, originalGuid);
            Assert.That(firstGuid, Is.EqualTo(originalGuid.ToString()));

            // Re-enrolling the same email with a freshly generated GUID must NOT change the account
            // GUID; the server returns the canonical (original) one so the client can reconcile.
            var reenrollController = ServiceProvider.GetService<SignUpController>();
            var differentGuid = Guid.NewGuid();
            reenrollController.SignUp(new SignupDto { DeviceId = deviceId, Email = email, UserGuid = differentGuid });
            var secondGuid = ConfirmAndGetAccountGuid(reenrollController, email, deviceId, differentGuid);

            Assert.That(secondGuid, Is.EqualTo(originalGuid.ToString()));
            Assert.That(secondGuid, Is.Not.EqualTo(differentGuid.ToString()));
        }

        [Test]
        public void ConfirmAddsDeviceAndLinksItToAccount()
        {
            var email = Guid.NewGuid() + "@passi.cloud";
            var deviceId = Guid.NewGuid().ToString();
            var userGuid = Guid.NewGuid();

            // Each call runs in its own DI scope / DbContext, mirroring separate HTTP requests.
            SignUpInScope(email, deviceId, userGuid);
            ConfirmInScope(email, deviceId, userGuid);

            using var scope = ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PassiDbContext>();

            var device = dbContext.Devices.FirstOrDefault(x => x.DeviceId == deviceId);
            Assert.That(device, Is.Not.Null, "A Device row should exist for the confirmed device");

            var user = dbContext.Users
                .Include(x => x.UserDevices).ThenInclude(x => x.Device)
                .First(x => x.EmailHash == email);

            Assert.That(
                user.UserDevices.Any(ud => ud.Device != null && ud.Device.DeviceId == deviceId),
                Is.True,
                "The confirmed device should be linked to the account via UserDevices");
            Assert.That(user.DeviceId, Is.EqualTo(device.Id),
                "The account's current device should point to the confirmed device");
        }

        [Test]
        public void ConfirmLinksNewDeviceWhenExistingUserReenrollsFromAnotherDevice()
        {
            var email = Guid.NewGuid() + "@passi.cloud";
            var firstDeviceId = Guid.NewGuid().ToString();
            var secondDeviceId = Guid.NewGuid().ToString();
            var userGuid = Guid.NewGuid();

            // First enrollment from device A.
            SignUpInScope(email, firstDeviceId, userGuid);
            ConfirmInScope(email, firstDeviceId, userGuid);

            // Existing user re-enrolls from a different device B.
            SignUpInScope(email, secondDeviceId, Guid.NewGuid());
            ConfirmInScope(email, secondDeviceId, userGuid);

            using var scope = ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PassiDbContext>();

            var user = dbContext.Users
                .Include(x => x.UserDevices).ThenInclude(x => x.Device)
                .First(x => x.EmailHash == email);

            var linkedDeviceIds = user.UserDevices
                .Where(ud => ud.Device != null)
                .Select(ud => ud.Device.DeviceId)
                .ToList();

            Assert.That(linkedDeviceIds, Does.Contain(firstDeviceId), "The original device should remain linked");
            Assert.That(linkedDeviceIds, Does.Contain(secondDeviceId), "The re-enrolled device should be linked to the account");
        }

        // SignUp dispatches the confirmation email on a background Task, so TestEmailSender.Code is
        // racy. The code is persisted synchronously during SignUp, so read it from the DB instead.
        private string GetLatestCode(string email)
        {
            using var scope = ServiceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            return repository.GetCode(email);
        }

        // Runs SignUp in its own DI scope / DbContext, mirroring a standalone HTTP request.
        private void SignUpInScope(string email, string deviceId, Guid userGuid)
        {
            using var scope = ServiceProvider.CreateScope();
            var controller = scope.ServiceProvider.GetRequiredService<SignUpController>();
            controller.SignUp(new SignupDto { DeviceId = deviceId, Email = email, UserGuid = userGuid });
        }

        // Runs Check + Confirm in their own DI scope / DbContext, mirroring a standalone HTTP request.
        private string ConfirmInScope(string email, string deviceId, Guid clientGuid)
        {
            var code = GetLatestCode(email);
            using var scope = ServiceProvider.CreateScope();
            var controller = scope.ServiceProvider.GetRequiredService<SignUpController>();
            controller.Check(new SignupCheckDto { Code = code, Username = email });

            var rsa = RSA.Create();
            var req = new CertificateRequest($"cn={email.Replace("@", "")}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

            var result = controller.Confirm(new SignupConfirmationDto
            {
                Code = code,
                DeviceId = deviceId,
                Email = email,
                Guid = clientGuid.ToString(),
                PublicCert = Convert.ToBase64String(cert.RawData)
            });

            var ok = result as Microsoft.AspNetCore.Mvc.OkObjectResult;
            Assert.That(ok, Is.Not.Null, "Confirm should return an OK body with the account GUID");
            var response = ok.Value as SignupConfirmationResponseDto;
            Assert.That(response, Is.Not.Null, "Confirm should return a SignupConfirmationResponseDto");
            return response.AccountGuid;
        }

        private string ConfirmAndGetAccountGuid(SignUpController controller, string email, string deviceId, Guid clientGuid)
        {
            var code = GetLatestCode(email);
            controller.Check(new SignupCheckDto { Code = code, Username = email });

            var rsa = RSA.Create();
            var req = new CertificateRequest($"cn={email.Replace("@", "")}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

            var result = controller.Confirm(new SignupConfirmationDto
            {
                Code = code,
                DeviceId = deviceId,
                Email = email,
                Guid = clientGuid.ToString(),
                PublicCert = Convert.ToBase64String(cert.RawData)
            });

            var ok = result as Microsoft.AspNetCore.Mvc.OkObjectResult;
            Assert.That(ok, Is.Not.Null, "Confirm should return an OK body with the account GUID");
            var response = ok.Value as SignupConfirmationResponseDto;
            Assert.That(response, Is.Not.Null, "Confirm should return a SignupConfirmationResponseDto");
            return response.AccountGuid;
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
                catch (Exception)
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