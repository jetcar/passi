using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using passi_webapi.Controllers;
using Repos;
using WebApiDto.SignUp;

namespace PassiWebApiTests.Repos
{
    public class CertificateRepositoryTests : TestBase
    {
        [Test]
        public void AddCertificateForANewDeviceLinksItToTheUsersDevices()
        {
            var signupController = ServiceProvider.GetRequiredService<SignUpController>();
            var email = Guid.NewGuid() + "@passi.cloud";
            var accountGuid = Guid.NewGuid();
            var signupDeviceId = Guid.NewGuid().ToString();
            var parentCert = ConfirmAccountOnDevice(signupController, email, accountGuid, signupDeviceId);

            // A brand-new device, enrolled by having the signup device sign its certificate (the
            // normal "add a device via certificate chaining" flow), not the device the user signed
            // up on.
            var newDeviceId = Guid.NewGuid().ToString();
            var childThumbprint = Guid.NewGuid().ToString();
            var certificateRepository = ServiceProvider.GetRequiredService<ICertificateRepository>();
            certificateRepository.AddCertificate(childThumbprint, "child-cert-data", parentCert.Thumbprint, newDeviceId);

            var userRepository = ServiceProvider.GetRequiredService<IUserRepository>();
            var devices = userRepository.GetAccountDevices(accountGuid, parentCert.Thumbprint);

            // AddCertificate enrolls the new device as the user's "current" device, but unless it is
            // also linked into UserDevices, the user can never see or remove it via normal device
            // management (AuthController.Devices / DeleteDevice only look at UserDevices).
            Assert.That(devices.Select(x => x.DeviceId), Does.Contain(newDeviceId));
        }

        [Test]
        public void ConcurrentAddCertificateCallsForSameNewDeviceIdProduceExactlyOneDeviceRow()
        {
            const int concurrency = 15;
            var signupController = ServiceProvider.GetRequiredService<SignUpController>();

            var parentThumbprints = new string[concurrency];
            for (var i = 0; i < concurrency; i++)
            {
                var email = Guid.NewGuid() + "@passi.cloud";
                var signupDeviceId = Guid.NewGuid().ToString();
                var cert = ConfirmAccountOnDevice(signupController, email, Guid.NewGuid(), signupDeviceId);
                parentThumbprints[i] = cert.Thumbprint;
            }

            // A brand-new device id never seen by any of the signups above: every thread below is
            // racing to be the first to create its Device row.
            var sharedDeviceId = Guid.NewGuid().ToString();

            using var barrier = new Barrier(concurrency);
            var exceptions = new ConcurrentBag<Exception>();
            var threads = new List<Thread>();

            for (var i = 0; i < concurrency; i++)
            {
                var parentThumbprint = parentThumbprints[i];
                var childThumbprint = Guid.NewGuid().ToString();

                var thread = new Thread(() =>
                {
                    try
                    {
                        using var scope = ServiceProvider.CreateScope();
                        var certificateRepository = scope.ServiceProvider.GetRequiredService<ICertificateRepository>();
                        barrier.SignalAndWait();
                        var strategy = certificateRepository.GetExecutionStrategy();
                        strategy.Execute(() =>
                        {
                            using var transaction = certificateRepository.BeginTransaction();
                            certificateRepository.AddCertificate(childThumbprint, "child-cert-data", parentThumbprint, sharedDeviceId);
                            transaction.Commit();
                        });
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
                threads.Add(thread);
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Assert.That(exceptions, Is.Empty,
                "No concurrent AddCertificate call for the same new device id should throw: " +
                string.Join(" | ", exceptions.Select(e => e.GetType().Name + ": " + e.Message)));

            using var verifyScope = ServiceProvider.CreateScope();
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<PassiDbContext>();
            var deviceCount = dbContext.Devices.Count(x => x.DeviceId == sharedDeviceId);

            Assert.That(deviceCount, Is.EqualTo(1),
                $"Expected exactly one Device row for device id {sharedDeviceId} but found {deviceCount}");
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
