using ConfigurationManager;
using Microsoft.Extensions.DependencyInjection;
using Models;
using NUnit.Framework;
using passi_webapi.Controllers;
using Repos;
using ServiceStack.Redis;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WebApiDto.SignUp;

namespace PassiWebApiTests.Repos
{
    public class SessionsRepositoryTests : TestBase
    {
        [Test]
        public void BeginSessionSetsRedisTtlToConfiguredTimeoutNotHardcodedFiveMinutes()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var sessionsRepository = ServiceProvider.GetService<ISessionsRepository>();
            var appSetting = ServiceProvider.GetService<AppSetting>();

            var email = Guid.NewGuid() + "@passi.cloud";
            var deviceId = Guid.NewGuid().ToString();
            ConfirmAccountOnDevice(signupController, email, Guid.NewGuid(), deviceId);

            var session = sessionsRepository.BeginSession(email, "SampleApp", "123456", "blue", "https://localhost/callback");

            var configuredTimeoutMinutes = Convert.ToInt32(appSetting["Timeout"]);
            var connectionString = $"{appSetting["redis"]}:{appSetting["redisPort"]}";
            using var redisManager = new RedisManagerPool(connectionString);
            using var redis = redisManager.GetClient();
            var key = typeof(SessionTempRecord).FullName + "." + session.Guid;
            var ttl = redis.GetTimeToLive(key);

            Assert.That(ttl, Is.Not.Null, "The session key should exist in Redis with a TTL set");
            // The cached session's TTL must track the configured session Timeout, not the
            // RedisService default of 5 minutes used when no explicit expiry is passed.
            Assert.That(ttl.Value.TotalSeconds, Is.EqualTo(configuredTimeoutMinutes * 60).Within(30),
                "The Redis TTL for a session should match the configured session Timeout");
        }

        [Test]
        public void CancelSessionMarksSessionAsCanceled()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var sessionsRepository = ServiceProvider.GetService<ISessionsRepository>();

            var email = Guid.NewGuid() + "@passi.cloud";
            var deviceId = Guid.NewGuid().ToString();
            ConfirmAccountOnDevice(signupController, email, Guid.NewGuid(), deviceId);

            var session = sessionsRepository.BeginSession(email, "SampleApp", "123456", "blue", "https://localhost/callback");

            sessionsRepository.CancelSession(session.Guid);

            var cancelled = sessionsRepository.CheckSessionAndReturnUser(session.Guid);
            Assert.That(cancelled, Is.Not.Null);
            Assert.That(cancelled.Status, Is.EqualTo(SessionStatus.Canceled));
        }

        [Test]
        public void VerifySessionDoesNotOverwriteACanceledSession()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var sessionsRepository = ServiceProvider.GetService<ISessionsRepository>();

            var email = Guid.NewGuid() + "@passi.cloud";
            var deviceId = Guid.NewGuid().ToString();
            var cert = ConfirmAccountOnDevice(signupController, email, Guid.NewGuid(), deviceId);

            var session = sessionsRepository.BeginSession(email, "SampleApp", "123456", "blue", "https://localhost/callback");

            sessionsRepository.CancelSession(session.Guid);

            // A late/racing confirmation must not resurrect a session the user already canceled.
            sessionsRepository.VerifySession(session.Guid, "signed-hash", cert.Thumbprint);

            var afterVerify = sessionsRepository.CheckSessionAndReturnUser(session.Guid);
            Assert.That(afterVerify, Is.Not.Null);
            Assert.That(afterVerify.Status, Is.EqualTo(SessionStatus.Canceled));
        }

        [Test]
        public void GetAuthorizedSessionReturnsNullWhenCertificateBelongsToAnotherUser()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var sessionsRepository = ServiceProvider.GetService<ISessionsRepository>();

            var ownerEmail = Guid.NewGuid() + "@passi.cloud";
            var ownerDeviceId = Guid.NewGuid().ToString();
            ConfirmAccountOnDevice(signupController, ownerEmail, Guid.NewGuid(), ownerDeviceId);

            var attackerEmail = Guid.NewGuid() + "@passi.cloud";
            var attackerDeviceId = Guid.NewGuid().ToString();
            var attackerCert = ConfirmAccountOnDevice(signupController, attackerEmail, Guid.NewGuid(), attackerDeviceId);

            var session = sessionsRepository.BeginSession(ownerEmail, "SampleApp", "123456", "blue", "https://localhost/callback");

            // The attacker owns a valid certificate, but not for the session's owner: access must be denied.
            var unauthorized = sessionsRepository.GetAuthorizedSession(session.Guid, attackerCert.Thumbprint, attackerEmail);

            Assert.That(unauthorized, Is.Null);
        }

        [Test]
        public void GetAuthorizedSessionReturnsSessionForItsOwner()
        {
            var signupController = ServiceProvider.GetService<SignUpController>();
            var sessionsRepository = ServiceProvider.GetService<ISessionsRepository>();

            var ownerEmail = Guid.NewGuid() + "@passi.cloud";
            var ownerDeviceId = Guid.NewGuid().ToString();
            var ownerCert = ConfirmAccountOnDevice(signupController, ownerEmail, Guid.NewGuid(), ownerDeviceId);

            var session = sessionsRepository.BeginSession(ownerEmail, "SampleApp", "123456", "blue", "https://localhost/callback");

            var authorized = sessionsRepository.GetAuthorizedSession(session.Guid, ownerCert.Thumbprint, ownerEmail);

            Assert.That(authorized, Is.Not.Null);
            Assert.That(authorized.Guid, Is.EqualTo(session.Guid));
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
