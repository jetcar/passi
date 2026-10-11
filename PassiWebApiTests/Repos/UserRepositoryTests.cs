using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Models;
using NUnit.Framework;
using passi_webapi.Controllers;
using Repos;
using WebApiDto.SignUp;

namespace PassiWebApiTests.Repos
{
    public class UserRepositoryTests : TestBase
    {
        [Test]
        public void UpdateNotificationTokenHandlesConcurrentFirstRegistrationForSameDevice()
        {
            var deviceId = Guid.NewGuid().ToString();
            const int concurrency = 15;
            var barrier = new Barrier(concurrency);

            var tasks = Enumerable.Range(0, concurrency).Select(i => Task.Run(() =>
            {
                using var scope = ServiceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                barrier.SignalAndWait();
                repo.UpdateNotificationToken(deviceId, $"token-{i}", "ios");
            })).ToArray();

            Assert.DoesNotThrow(() => Task.WaitAll(tasks));

            using var verifyScope = ServiceProvider.CreateScope();
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<PassiDbContext>();
            var devices = dbContext.Devices.Where(x => x.DeviceId == deviceId).ToList();
            Assert.That(devices, Has.Count.EqualTo(1));
        }

        [Test]
        public void ConfirmInvitationWithNoMatchingInvitationDoesNotPersistADeviceRow()
        {
            using var scope = ServiceProvider.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var deviceId = Guid.NewGuid().ToString();

            // No invitation anywhere matches this email/code combination, so confirmation must fail
            // without any side effect - in particular, it must not create a Device row for a device
            // id that was only ever seen on this failed attempt.
            var result = userRepository.ConfirmInvitation(
                Guid.NewGuid() + "@passi.cloud", "irrelevant-cert", Guid.NewGuid().ToString(), "wrong-code", deviceId);

            Assert.That(result, Is.EqualTo(Guid.Empty));

            using var verifyScope = ServiceProvider.CreateScope();
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<PassiDbContext>();
            var deviceCount = dbContext.Devices.Count(x => x.DeviceId == deviceId);
            Assert.That(deviceCount, Is.EqualTo(0),
                $"Expected no Device row for device id {deviceId} after a failed confirmation but found {deviceCount}");
        }

        // GetOrCreateDevice (private, used by AddUser/SignUp and ConfirmInvitation/Confirm) does a
        // classic check-then-act: look up the Device row by DeviceId, and if none is found, add a new
        // one. When several requests race for the very first signup on a brand-new device id, more
        // than one can see "no row yet" and each add its own Device, leaving duplicate Device rows for
        // the same device id (or worse, surfacing as a thrown exception from SaveChanges) instead of
        // reusing the row a concurrent request already created.
        [Test]
        public void ConcurrentSignUpsForSameNewDeviceIdProduceExactlyOneDeviceRow()
        {
            var deviceId = Guid.NewGuid().ToString();
            const int concurrency = 15;

            using var barrier = new Barrier(concurrency);
            var exceptions = new ConcurrentBag<Exception>();
            var threads = new List<Thread>();

            for (var i = 0; i < concurrency; i++)
            {
                var signupDto = new SignupDto
                {
                    DeviceId = deviceId,
                    Email = Guid.NewGuid() + "@passi.cloud",
                    UserGuid = Guid.NewGuid()
                };

                var thread = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        using var scope = ServiceProvider.CreateScope();
                        var controller = scope.ServiceProvider.GetRequiredService<SignUpController>();
                        controller.SignUp(signupDto);
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
                "No concurrent SignUp call for the same new device id should throw: " +
                string.Join(" | ", exceptions.Select(e => e.GetType().Name + ": " + e.Message)));

            using var verifyScope = ServiceProvider.CreateScope();
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<PassiDbContext>();
            var deviceCount = dbContext.Devices.Count(x => x.DeviceId == deviceId);

            Assert.That(deviceCount, Is.EqualTo(1),
                $"Expected exactly one Device row for device id {deviceId} but found {deviceCount}");
        }

        [Test]
        public void ConfirmInvitationDisposesTheCertificateLoadedToReadItsThumbprint()
        {
            using var scope = ServiceProvider.CreateScope();
            var userRepository = (UserRepository)scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var email = Guid.NewGuid() + "@passi.cloud";
            const string code = "123456";
            var deviceId = Guid.NewGuid().ToString();
            var user = new UserDb
            {
                EmailHash = email,
                Guid = Guid.NewGuid(),
                Device = new DeviceDb { DeviceId = deviceId },
            };
            user.Invitations.Add(new UserInvitationDb { Code = code });
            userRepository.AddUser(user);

            var cert = CreateCertificate();
            var trackingCert = new DisposeTrackingCertificate(cert.RawData);
            var previousLoader = UserRepository.LoadCertificate;
            UserRepository.LoadCertificate = _ => trackingCert;
            try
            {
                userRepository.ConfirmInvitation(email, Convert.ToBase64String(cert.RawData), Guid.NewGuid().ToString(), code, deviceId);
            }
            finally
            {
                UserRepository.LoadCertificate = previousLoader;
            }

            // ConfirmInvitation loads a certificate purely to read its Thumbprint; nothing else
            // owns it, so it must dispose it instead of leaking the native handle.
            Assert.That(trackingCert.WasDisposed, Is.True);
        }

        private static X509Certificate2 CreateCertificate()
        {
            using var rsa = RSA.Create();
            var request = new CertificateRequest($"cn={Guid.NewGuid()}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        }

        // Lets the test observe whether ConfirmInvitation disposes the certificate it loaded.
        private class DisposeTrackingCertificate : X509Certificate2
        {
            public bool WasDisposed { get; private set; }

            public DisposeTrackingCertificate(byte[] rawData) : base(rawData)
            {
            }

            protected override void Dispose(bool disposing)
            {
                WasDisposed = true;
                base.Dispose(disposing);
            }
        }

        // EmailHash is the plain (trimmed) email, not an actual hash, and mobile keyboards commonly
        // auto-capitalize the first letter of an email field on signup. A later login attempt with the
        // email typed in a different case must still find the account instead of reporting it as
        // unregistered.
        [Test]
        public void IsUsernameTakenMatchesEmailRegardlessOfCase()
        {
            using var scope = ServiceProvider.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var email = $"User.{Guid.NewGuid()}@Passi.Cloud";

            userRepository.AddUser(new UserDb
            {
                EmailHash = email,
                Guid = Guid.NewGuid(),
                Device = new DeviceDb { DeviceId = Guid.NewGuid().ToString() }
            });

            Assert.That(userRepository.IsUsernameTaken(email.ToLowerInvariant()), Is.True,
                "A signup stored as mixed-case must still be found when logging in with the lower-case form of the same email.");
        }
    }
}
