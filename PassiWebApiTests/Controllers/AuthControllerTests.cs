using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Models;
using NUnit.Framework;
using passi_webapi.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WebApiDto.Auth;
using WebApiDto.SignUp;

namespace PassiWebApiTests.Controllers
{
    public class AuthControllerTests : TestBase
    {
        [Test]
        public void DevicesListsAllConfirmedDevicesAndDeletesNonCurrentDevice()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var authController = ServiceProvider.GetService<AuthController>();
            var email = Guid.NewGuid() + "@passi.cloud";
            var accountGuid = Guid.NewGuid();
            var primaryDeviceId = Guid.NewGuid().ToString();
            var secondaryDeviceId = Guid.NewGuid().ToString();

            var primaryCert = ConfirmAccountOnDevice(signupController, email, accountGuid, primaryDeviceId);
            ConfirmAccountOnDevice(signupController, email, accountGuid, secondaryDeviceId);

            var deviceResult = authController.Devices(new ManageDevicesDto
            {
                AccountGuid = accountGuid,
                Thumbprint = primaryCert.Thumbprint,
                CurrentDeviceId = secondaryDeviceId,
            });

            var devices = ((OkObjectResult)deviceResult.Result).Value as System.Collections.Generic.List<WebApiDto.Auth.Dto.ManagedDeviceDto>;
            Assert.That(devices, Has.Count.EqualTo(2));
            Assert.That(devices.Count(x => x.IsCurrent), Is.EqualTo(1));
            Assert.That(devices.Single(x => x.IsCurrent).DeviceId, Is.EqualTo(secondaryDeviceId));

            var deleteResponse = authController.DeleteDevice(new DeleteDeviceDto
            {
                AccountGuid = accountGuid,
                Thumbprint = primaryCert.Thumbprint,
                CurrentDeviceId = secondaryDeviceId,
                DeviceId = primaryDeviceId,
            });

            Assert.That(deleteResponse, Is.Not.Null);

            var afterDeleteResult = authController.Devices(new ManageDevicesDto
            {
                AccountGuid = accountGuid,
                Thumbprint = primaryCert.Thumbprint,
                CurrentDeviceId = secondaryDeviceId,
            });
            var afterDelete = ((OkObjectResult)afterDeleteResult.Result).Value as System.Collections.Generic.List<WebApiDto.Auth.Dto.ManagedDeviceDto>;

            Assert.That(afterDelete, Has.Count.EqualTo(1));
            Assert.That(afterDelete.Single().DeviceId, Is.EqualTo(secondaryDeviceId));
        }

        [Test]
        public void DeleteDeviceRejectsCurrentDevice()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var authController = ServiceProvider.GetService<AuthController>();
            var email = Guid.NewGuid() + "@passi.cloud";
            var accountGuid = Guid.NewGuid();
            var deviceId = Guid.NewGuid().ToString();

            ConfirmAccountOnDevice(signupController, email, accountGuid, deviceId);
            var cert = CreateCertificate(email);

            var exception = Assert.Throws<ClientException>(() => authController.DeleteDevice(new DeleteDeviceDto
            {
                AccountGuid = accountGuid,
                Thumbprint = cert.Thumbprint,
                CurrentDeviceId = deviceId,
                DeviceId = deviceId,
            }));

            Assert.That(exception?.Message, Is.EqualTo("Current device cannot be removed"));
        }

        [Test]
        public void StartLoginUsesRegisteredDevicesEvenWhenNoInvitationIsCurrentlyConfirmed()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var authController = ServiceProvider.GetService<AuthController>();
            var email = Guid.NewGuid() + "@passi.cloud";
            var accountGuid = Guid.NewGuid();
            var verifiedDeviceId = Guid.NewGuid().ToString();

            ConfirmAccountOnDevice(signupController, email, accountGuid, verifiedDeviceId);

            signupController.SignUp(new SignupDto
            {
                Email = email,
                UserGuid = accountGuid,
                DeviceId = Guid.NewGuid().ToString(),
            });

            var response = authController.Start(new StartLoginDto
            {
                Username = email,
                ClientId = "SampleApp",
                ReturnUrl = "https://localhost/callback",
                RandomString = "123456",
                CheckColor = Color.blue,
            });

            var result = response as OkObjectResult;
            Assert.That(result, Is.Not.Null);

            var payload = result!.Value as LoginResponceDto;
            Assert.That(payload, Is.Not.Null);
            Assert.That(payload!.SessionId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(payload.RegisteredDevices, Is.Not.Null);
            Assert.That(payload.RegisteredDevices, Has.Count.EqualTo(1));
            Assert.That(payload.RegisteredDevices.Single(), Does.StartWith("Registered device ("));
        }

        [Test]
        public void StartLoginStillWorksWhenSameEmailIsRegisteredFromAnotherDeviceButNotYetConfirmed()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var authController = ServiceProvider.GetService<AuthController>();
            var email = Guid.NewGuid() + "@passi.cloud";
            var accountGuid = Guid.NewGuid();
            var firstDeviceId = Guid.NewGuid().ToString();
            var secondDeviceId = Guid.NewGuid().ToString();

            ConfirmAccountOnDevice(signupController, email, accountGuid, firstDeviceId);

            signupController.SignUp(new SignupDto
            {
                Email = email,
                UserGuid = accountGuid,
                DeviceId = secondDeviceId,
            });

            var response = authController.Start(new StartLoginDto
            {
                Username = email,
                ClientId = "SampleApp",
                ReturnUrl = "https://localhost/callback",
                RandomString = "123456",
                CheckColor = Color.blue,
            });

            var result = response as OkObjectResult;
            Assert.That(result, Is.Not.Null);

            var payload = result!.Value as LoginResponceDto;
            Assert.That(payload, Is.Not.Null);
            Assert.That(payload!.SessionId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(payload.RegisteredDevices, Has.Count.EqualTo(1));
            Assert.That(payload.RegisteredDevices.Single(), Is.EqualTo(GetExpectedDeviceDisplayName(firstDeviceId)));
        }

        [Test]
        public void StartLoginReturnsBothDevicesAfterSameEmailIsConfirmedOnSecondDevice()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var authController = ServiceProvider.GetService<AuthController>();
            var email = Guid.NewGuid() + "@passi.cloud";
            var accountGuid = Guid.NewGuid();
            var firstDeviceId = Guid.NewGuid().ToString();
            var secondDeviceId = Guid.NewGuid().ToString();

            ConfirmAccountOnDevice(signupController, email, accountGuid, firstDeviceId);
            ConfirmAccountOnDevice(signupController, email, accountGuid, secondDeviceId);

            var response = authController.Start(new StartLoginDto
            {
                Username = email,
                ClientId = "SampleApp",
                ReturnUrl = "https://localhost/callback",
                RandomString = "123456",
                CheckColor = Color.blue,
            });

            var result = response as OkObjectResult;
            Assert.That(result, Is.Not.Null);

            var payload = result!.Value as LoginResponceDto;
            Assert.That(payload, Is.Not.Null);
            Assert.That(payload!.SessionId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(payload.RegisteredDevices, Has.Count.EqualTo(2));
            Assert.That(payload.RegisteredDevices, Does.Contain(GetExpectedDeviceDisplayName(firstDeviceId)));
            Assert.That(payload.RegisteredDevices, Does.Contain(GetExpectedDeviceDisplayName(secondDeviceId)));
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

        private static string GetExpectedDeviceDisplayName(string deviceId)
        {
            var shortIdentifier = deviceId.Length <= 10
                ? deviceId
                : deviceId[..8];

            return $"Registered device ({shortIdentifier})";
        }

        private static X509Certificate2 CreateCertificate(string email)
        {
            using var rsa = RSA.Create();
            var request = new CertificateRequest($"cn={email.Replace("@", "")}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        }
    }
}