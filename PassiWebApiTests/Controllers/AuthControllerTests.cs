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
using System.Text;
using System.Threading;
using WebApiDto.Auth;
using WebApiDto.Auth.Dto;
using WebApiDto.SignUp;
using Repos;
using WebApiDto;

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

        // CleanupDeviceIfUnused (private, called from DeleteDevice) does a check-then-act with no
        // lock: it reads whether any UserDevices/Users row still references the device, and only
        // deletes the Device row when none does. GetOrCreateDevice guards the mirror-image race
        // with a pg_advisory_xact_lock, but CleanupDeviceIfUnused has none. A SignUp racing in
        // between the "is it referenced" check and the delete can attach a brand-new link to the
        // device just before it is deleted out from under it; since UserDeviceDb.Device and
        // UserDb.Device both cascade-delete, that new link - and the user row it belongs to,
        // because this scenario signs the user up for the very first time on that device - is
        // silently removed along with the Device row.
        [Test]
        public void DeleteDeviceCleanupDoesNotLoseAConcurrentNewSignUpOnTheSameDevice()
        {
            const int attempts = 20;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var sharedDeviceId = Guid.NewGuid().ToString();
                var keepDeviceId = Guid.NewGuid().ToString();
                var email = Guid.NewGuid() + "@passi.cloud";
                var accountGuid = Guid.NewGuid();
                var newEmail = Guid.NewGuid() + "@passi.cloud";

                using var setupScope = ServiceProvider.CreateScope();
                var setupSignup = setupScope.ServiceProvider.GetRequiredService<SignUpController>();

                // sharedDeviceId starts out referenced by exactly one link (the one DeleteDevice is
                // about to remove), so the cleanup's "is it referenced" check sees nothing left and
                // proceeds to delete the Device row - precisely the window the race needs.
                var cert = ConfirmAccountOnDevice(setupSignup, email, accountGuid, sharedDeviceId);
                ConfirmAccountOnDevice(setupSignup, email, accountGuid, keepDeviceId);

                using var barrier = new Barrier(2);
                Exception deleteException = null;
                Exception signUpException = null;

                var deleteThread = new Thread(() =>
                {
                    try
                    {
                        using var scope = ServiceProvider.CreateScope();
                        var authController = scope.ServiceProvider.GetRequiredService<AuthController>();
                        barrier.SignalAndWait();
                        authController.DeleteDevice(new DeleteDeviceDto
                        {
                            AccountGuid = accountGuid,
                            Thumbprint = cert.Thumbprint,
                            CurrentDeviceId = keepDeviceId,
                            DeviceId = sharedDeviceId,
                        });
                    }
                    catch (Exception ex)
                    {
                        deleteException = ex;
                    }
                });

                var signUpThread = new Thread(() =>
                {
                    try
                    {
                        using var scope = ServiceProvider.CreateScope();
                        var signupController = scope.ServiceProvider.GetRequiredService<SignUpController>();
                        barrier.SignalAndWait();
                        signupController.SignUp(new SignupDto
                        {
                            Email = newEmail,
                            UserGuid = Guid.NewGuid(),
                            DeviceId = sharedDeviceId,
                        });
                    }
                    catch (Exception ex)
                    {
                        signUpException = ex;
                    }
                });

                deleteThread.Start();
                signUpThread.Start();
                deleteThread.Join();
                signUpThread.Join();

                Assert.That(deleteException, Is.Null,
                    $"Attempt {attempt}: DeleteDevice should not throw: {deleteException}");
                Assert.That(signUpException, Is.Null,
                    $"Attempt {attempt}: a SignUp racing a DeleteDevice cleanup of the same device should not throw: {signUpException}");

                using var verifyScope = ServiceProvider.CreateScope();
                var userRepository = verifyScope.ServiceProvider.GetRequiredService<IUserRepository>();
                Assert.That(userRepository.IsUsernameTaken(newEmail), Is.True,
                    $"Attempt {attempt}: the user signed up on device {sharedDeviceId} must survive a concurrent " +
                    "DeleteDevice cleanup of that same device.");
            }
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

        [Test]
        public void ActiveSessionReturnsTwoDigitNumberAndKeepsColorForOlderApps()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var authController = ServiceProvider.GetService<AuthController>();
            var email = Guid.NewGuid() + "@passi.cloud";
            var deviceId = Guid.NewGuid().ToString();
            ConfirmAccountOnDevice(signupController, email, Guid.NewGuid(), deviceId);

            var start = authController.Start(new StartLoginDto
            {
                Username = email,
                ClientId = "SampleApp",
                ReturnUrl = "https://localhost/callback",
                RandomString = "123456",
                CheckColor = Color.green,
                CheckNumber = 42,
            }) as OkObjectResult;
            Assert.That(start, Is.Not.Null);

            var active = authController.GetActiveSession(new GetAllSessionDto { DeviceId = deviceId }) as OkObjectResult;

            var notification = active!.Value as NotificationDto;
            Assert.That(notification, Is.Not.Null);
            Assert.That(notification!.ConfirmationNumber, Is.EqualTo(42));
            Assert.That(notification.ConfirmationColor, Is.EqualTo(Color.green));
        }

        [Test]
        public void AuthorizeConfirmsSessionWhenSignatureIsValid()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var authController = ServiceProvider.GetService<AuthController>();
            var email = Guid.NewGuid() + "@passi.cloud";
            var deviceId = Guid.NewGuid().ToString();
            var cert = ConfirmAccountOnDevice(signupController, email, Guid.NewGuid(), deviceId);

            var start = authController.Start(new StartLoginDto
            {
                Username = email,
                ClientId = "SampleApp",
                ReturnUrl = "https://localhost/callback",
                RandomString = "123456",
                CheckColor = Color.blue,
            }) as OkObjectResult;
            var sessionId = ((LoginResponceDto)start!.Value).SessionId;

            var authorizeResult = authController.Authorize(new AuthorizeDto
            {
                SessionId = sessionId,
                SignedHash = SignData("123456", cert),
                PublicCertThumbprint = cert.Thumbprint,
            });

            Assert.That(authorizeResult, Is.InstanceOf<OkObjectResult>());

            var checkResult = authController.Check(sessionId);
            Assert.That(checkResult, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public void AuthorizeRejectsSignatureFromAnUnregisteredCertificate()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var authController = ServiceProvider.GetService<AuthController>();
            var email = Guid.NewGuid() + "@passi.cloud";
            var deviceId = Guid.NewGuid().ToString();
            var cert = ConfirmAccountOnDevice(signupController, email, Guid.NewGuid(), deviceId);

            var start = authController.Start(new StartLoginDto
            {
                Username = email,
                ClientId = "SampleApp",
                ReturnUrl = "https://localhost/callback",
                RandomString = "123456",
                CheckColor = Color.blue,
            }) as OkObjectResult;
            var sessionId = ((LoginResponceDto)start!.Value).SessionId;

            // Valid RSA signature, but from a key never registered for this account.
            var attackerCert = CreateCertificate(email);

            var authorizeResult = authController.Authorize(new AuthorizeDto
            {
                SessionId = sessionId,
                SignedHash = SignData("123456", attackerCert),
                PublicCertThumbprint = cert.Thumbprint,
            });

            Assert.That(authorizeResult, Is.InstanceOf<BadRequestObjectResult>());

            var checkResult = authController.Check(sessionId) as BadRequestObjectResult;
            Assert.That(checkResult, Is.Not.Null);
            Assert.That(checkResult!.Value, Is.EqualTo("Waiting for response"));
        }

        [Test]
        public void SessionReturnsTheRandomStringThePhoneSigned()
        {
            // OpenIDC verifies the signature against this value; plain OAuth clients (e.g. MCP agents) send no nonce.
            var signupController = ServiceProvider.GetService<SignUpController>();
            var authController = ServiceProvider.GetService<AuthController>();
            var email = Guid.NewGuid() + "@passi.cloud";
            var deviceId = Guid.NewGuid().ToString();
            var cert = ConfirmAccountOnDevice(signupController, email, Guid.NewGuid(), deviceId);

            var start = authController.Start(new StartLoginDto
            {
                Username = email,
                ClientId = "SampleApp",
                ReturnUrl = "https://localhost/callback",
                RandomString = "7418529630",
                CheckColor = Color.blue,
            }) as OkObjectResult;
            var sessionId = ((LoginResponceDto)start!.Value).SessionId;
            authController.Authorize(new AuthorizeDto
            {
                SessionId = sessionId,
                SignedHash = SignData("7418529630", cert),
                PublicCertThumbprint = cert.Thumbprint,
            });

            var session = (authController.Session(sessionId, cert.Thumbprint, email) as OkObjectResult)?.Value as SessionMinDto;

            Assert.That(session, Is.Not.Null);
            Assert.That(session!.RandomString, Is.EqualTo("7418529630"));
        }

        private static string SignData(string data, X509Certificate2 certificate)
        {
            using var sha512 = SHA512.Create();
            var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(data));
            var signedBytes = certificate.GetRSAPrivateKey().SignHash(hash, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signedBytes);
        }

        private X509Certificate2 ConfirmAccountOnDevice(SignUpController signupController, string email, Guid accountGuid, string deviceId)
        {
            signupController.SignUp(new SignupDto
            {
                Email = email,
                UserGuid = accountGuid,
                DeviceId = deviceId,
            });

            var cert = CreateCertificate(email);
            string code;
            using (var scope = ServiceProvider.CreateScope())
                code = scope.ServiceProvider.GetRequiredService<IUserRepository>().GetCode(email);
            signupController.Confirm(new SignupConfirmationDto
            {
                Code = code,
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