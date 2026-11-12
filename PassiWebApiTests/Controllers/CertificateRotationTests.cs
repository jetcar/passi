using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Models;
using NUnit.Framework;
using passi_webapi.Controllers;
using Repos;
using Services;
using WebApiDto.Certificate;
using WebApiDto.SignUp;
using WebApiDto.Auth;

namespace PassiWebApiTests.Controllers;

public class CertificateRotationTests : TestBase
{
    [Test]
    public void AcceptedEnrollmentCanBeRetriedButConsumedCodeCannotEnrollAnotherKeyOrDevice()
    {
        using var scope = ServiceProvider.CreateScope();
        var signup = scope.ServiceProvider.GetRequiredService<SignUpController>();
        var email = $"{Guid.NewGuid()}@passi.cloud";
        var accountGuid = Guid.NewGuid();
        var deviceId = Guid.NewGuid().ToString();
        signup.SignUp(new SignupDto { Email = email, UserGuid = accountGuid, DeviceId = deviceId });
        using var certificate = CreateCertificate(email.Replace("@", ""));
        var request = new SignupConfirmationDto
        {
            Code = scope.ServiceProvider.GetRequiredService<IUserRepository>().GetCode(email),
            DeviceId = deviceId, Email = email, Guid = accountGuid.ToString(),
            PublicCert = Convert.ToBase64String(certificate.RawData)
        };
        signup.Confirm(request);
        using var retryScope = ServiceProvider.CreateScope();
        var retryController = retryScope.ServiceProvider.GetRequiredService<SignUpController>();
        var retry = (OkObjectResult)retryController.Confirm(request);
        Assert.That(((SignupConfirmationResponseDto)retry.Value).AccountGuid, Is.EqualTo(accountGuid.ToString()));
        request.DeviceId = Guid.NewGuid().ToString();
        Assert.Throws<BadRequestException>(() => retryController.Confirm(request));
        request.DeviceId = deviceId;
        using var anotherKey = CreateCertificate(email.Replace("@", ""));
        request.PublicCert = Convert.ToBase64String(anotherKey.RawData);
        Assert.Throws<BadRequestException>(() => retryController.Confirm(request));
        var db = retryScope.ServiceProvider.GetRequiredService<PassiDbContext>();
        Assert.That(db.Certificates.Count(x => x.User.EmailHash == email), Is.EqualTo(1));
    }

    [Test]
    public void MigrationRetiresOnlyOldAuthenticationKeyAndRetryIsSafe()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            request.RetireParent = true;
            request.ParentCertHashSignature = Sign(parent, request.SigningPayload());
            var controller = scope.ServiceProvider.GetRequiredService<CertificateController>();
            controller.UpdatePublicCert(request);
            controller.UpdatePublicCert(request);
            var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            var oldRecord = db.Certificates.AsNoTracking().Single(x => x.Thumbprint == parent.Thumbprint);
            var newRecord = db.Certificates.AsNoTracking().Single(x => x.ParentCertId == parent.Thumbprint);
            Assert.That(oldRecord.IsRetired, Is.True);
            Assert.That(newRecord.IsRetired, Is.False);
            var email = db.Users.Where(x => x.Id == oldRecord.UserId).Select(x => x.EmailHash).Single();
            var sessions = scope.ServiceProvider.GetRequiredService<ISessionsRepository>();
            Assert.That(sessions.GetCertificate(email, parent.Thumbprint), Is.Null);
            Assert.That(sessions.GetCertificate(email, newRecord.Thumbprint), Is.Not.Null);
            Assert.That(controller.PublicCert(parent.Thumbprint, email).PublicCert, Is.Null);
            var session = sessions.BeginSession(email, "rotation-test", "test-only-random-challenge", "Red", "https://example.test");
            var authorization = scope.ServiceProvider.GetRequiredService<AuthController>().Authorize(new AuthorizeDto
            {
                SessionId = session.Guid, PublicCertThumbprint = parent.Thumbprint,
                SignedHash = Sign(parent, session.RandomString)
            });
            Assert.That(authorization, Is.InstanceOf<BadRequestObjectResult>());
            Assert.That(sessions.GetAuthorizedSession(session.Guid, parent.Thumbprint, email), Is.Null);
        }
    }

    [Test]
    public void ConcurrentMigrationRetriesReturnOneAcceptedSuccessor()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        {
            request.RetireParent = true;
            request.ParentCertHashSignature = Sign(parent, request.SigningPayload());
            using var start = new Barrier(4);
            var attempts = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
            {
                using var scope = ServiceProvider.CreateScope();
                start.SignalAndWait();
                return scope.ServiceProvider.GetRequiredService<CertificateController>().UpdatePublicCert(request).PublicCert;
            })).ToArray();
            Task.WaitAll(attempts);
            Assert.That(attempts.Select(x => x.Result), Is.All.EqualTo(request.PublicCert));
            using var verifyScope = ServiceProvider.CreateScope();
            var db = verifyScope.ServiceProvider.GetRequiredService<PassiDbContext>();
            Assert.That(db.Certificates.Count(x => x.ParentCertId == parent.Thumbprint), Is.EqualTo(1));
        }
    }

    [Test]
    public void ParentThatPreviouslyEnrolledAnotherDeviceCanMigrateWithoutRetiringThatDevice()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            var controller = scope.ServiceProvider.GetRequiredService<CertificateController>();
            var originalDeviceId = request.DeviceId;
            request.DeviceId = Guid.NewGuid().ToString();
            controller.UpdatePublicCert(request);
            var earlierChild = request.PublicCert;
            var earlierDeviceId = request.DeviceId;
            var earlierSignature = request.ParentCertHashSignature;
            using var migrated = CreateCertificate(parent.GetNameInfo(X509NameType.SimpleName, true));
            request.PublicCert = Convert.ToBase64String(migrated.RawData);
            request.DeviceId = originalDeviceId;
            request.RetireParent = true;
            request.ParentCertHashSignature = Sign(parent, request.SigningPayload());
            controller.UpdatePublicCert(request);
            controller.UpdatePublicCert(request);
            var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            Assert.That(db.Certificates.Count(x => x.ParentCertId == parent.Thumbprint), Is.EqualTo(2));
            Assert.That(db.Certificates.Single(x => x.PublicCert == earlierChild).IsRetired, Is.False);
            Assert.That(db.Certificates.Single(x => x.Thumbprint == parent.Thumbprint).IsRetired, Is.True);
            var replay = new CertificateUpdateDto
            {
                PublicCert = earlierChild, ParentCertThumbprint = parent.Thumbprint,
                DeviceId = earlierDeviceId, ParentCertHashSignature = earlierSignature
            };
            Assert.That(controller.UpdatePublicCert(replay).PublicCert, Is.EqualTo(earlierChild));
            // A later signature cannot relabel an older enrollment as the accepted retirement.
            replay.RetireParent = true;
            replay.ParentCertHashSignature = Sign(parent, replay.SigningPayload());
            Assert.Throws<ClientException>(() => controller.UpdatePublicCert(replay));
            // Nor can a different device claim the successful retirement receipt.
            request.DeviceId = earlierDeviceId;
            request.ParentCertHashSignature = Sign(parent, request.SigningPayload());
            Assert.Throws<ClientException>(() => controller.UpdatePublicCert(request));
            Assert.That(db.Users.Single(x => x.Certificates.Any(c => c.Thumbprint == parent.Thumbprint))
                .Device.DeviceId, Is.EqualTo(originalDeviceId));
        }
    }

    [Test]
    public void RetirementDeviceIdIsBoundToSignature()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            request.RetireParent = true;
            request.ParentCertHashSignature = Sign(parent, request.SigningPayload());
            request.DeviceId = Guid.NewGuid().ToString();
            Assert.Throws<BadRequestException>(() => scope.ServiceProvider.GetRequiredService<CertificateController>()
                .UpdatePublicCert(request));
            var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            Assert.That(db.Certificates.Single(x => x.Thumbprint == parent.Thumbprint).IsRetired, Is.False);
            Assert.That(db.Devices.Any(x => x.DeviceId == request.DeviceId), Is.False);
        }
    }

    [Test]
    public void VersionOneRetirementSignatureWithoutDeviceBindingIsRejected()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            request.RetireParent = true;
            request.ParentCertHashSignature = Sign(parent,
                $"Passi:RetireParent:v1\n{request.ParentCertThumbprint}\n{request.PublicCert}");
            Assert.Throws<BadRequestException>(() => scope.ServiceProvider.GetRequiredService<CertificateController>()
                .UpdatePublicCert(request));
            var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            Assert.That(db.Certificates.Single(x => x.Thumbprint == parent.Thumbprint).IsRetired, Is.False);
        }
    }

    [TestCase("device\nline")]
    [TestCase("device\rline")]
    [TestCase("dévïce")]
    public void RetirementRejectsAmbiguousDeviceIdentifierEncoding(string deviceId)
    {
        var (parent, request) = CreateRotation();
        using (parent)
        {
            request.RetireParent = true;
            request.DeviceId = deviceId;
            request.ParentCertHashSignature = Sign(parent, request.SigningPayload());
            var parentRecord = new CertificateDb { PublicCert = Convert.ToBase64String(parent.RawData) };
            var error = Assert.Throws<BadRequestException>(() => new CertValidator().ValidateCertificateChain(request, parentRecord));
            Assert.That(error.Message, Is.EqualTo("Invalid retirement device identifier"));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CorrectlySignedRetirementCannotRegisterAnUnlinkedDevice(bool belongsToAnotherAccount)
    {
        var (parent, request) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            request.RetireParent = true;
            request.DeviceId = Guid.NewGuid().ToString();
            if (belongsToAnotherAccount)
            {
                var (otherParent, otherRequest) = CreateRotation();
                using (otherParent)
                    request.DeviceId = otherRequest.DeviceId;
            }
            request.ParentCertHashSignature = Sign(parent, request.SigningPayload());
            Assert.Throws<ClientException>(() => scope.ServiceProvider.GetRequiredService<CertificateController>()
                .UpdatePublicCert(request));
            var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            Assert.That(db.Certificates.Single(x => x.Thumbprint == parent.Thumbprint).IsRetired, Is.False);
            Assert.That(db.Certificates.Any(x => x.ParentCertId == parent.Thumbprint), Is.False);
            Assert.That(db.Devices.Any(x => x.DeviceId == request.DeviceId), Is.EqualTo(belongsToAnotherAccount));
        }
    }

    [Test]
    public void MissingParentInRepositoryReturnsControlledError()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            var controller = scope.ServiceProvider.GetRequiredService<CertificateController>();
            controller.UpdatePublicCert(request);
            var repo = scope.ServiceProvider.GetRequiredService<ICertificateRepository>();
            using var child = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(request.PublicCert));
            var error = Assert.Throws<ClientException>(() => repo.AddCertificate(child.Thumbprint,
                request.PublicCert, "missing-parent", request.DeviceId));
            Assert.That(error.Message, Is.EqualTo("Parent certificate not found"));
        }
    }

    [Test]
    public void LoginCannotStartWhenAllAccountCertificatesAreRetired()
    {
        var (parent, _) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            var record = db.Certificates.Include(x => x.User).Single(x => x.Thumbprint == parent.Thumbprint);
            record.IsRetired = true;
            db.SaveChanges();
            var response = scope.ServiceProvider.GetRequiredService<AuthController>().Start(new StartLoginDto
            {
                Username = record.User.EmailHash, ClientId = "review-test", RandomString = "test-only-challenge",
                ReturnUrl = "https://example.test"
            });
            Assert.That(response, Is.InstanceOf<BadRequestObjectResult>());
            Assert.That(db.Sessions.Any(x => x.UserId == record.UserId), Is.False);
        }
    }

    [Test]
    public void CompetingDifferentMigrationSuccessorsCannotBothBeAccepted()
    {
        var (parent, first) = CreateRotation();
        using (parent)
        using (var nextKey = CreateCertificate(parent.GetNameInfo(X509NameType.SimpleName, true)))
        {
            first.RetireParent = true;
            first.ParentCertHashSignature = Sign(parent, first.SigningPayload());
            var second = new CertificateUpdateDto
            {
                PublicCert = Convert.ToBase64String(nextKey.RawData), ParentCertThumbprint = parent.Thumbprint,
                DeviceId = first.DeviceId, RetireParent = true
            };
            second.ParentCertHashSignature = Sign(parent, second.SigningPayload());
            using var start = new Barrier(2);
            var attempts = new[] { first, second }.Select(request => Task.Run(() =>
            {
                using var scope = ServiceProvider.CreateScope();
                start.SignalAndWait();
                try
                {
                    scope.ServiceProvider.GetRequiredService<CertificateController>().UpdatePublicCert(request);
                    return true;
                }
                catch (ClientException) { return false; }
            })).ToArray();
            Task.WaitAll(attempts);
            Assert.That(attempts.Count(x => x.Result), Is.EqualTo(1));
            using var verifyScope = ServiceProvider.CreateScope();
            Assert.That(verifyScope.ServiceProvider.GetRequiredService<PassiDbContext>()
                .Certificates.Count(x => x.ParentCertId == parent.Thumbprint), Is.EqualTo(1));
        }
    }

    [Test]
    public void SchemaRollbackRefusesToReactivateRetiredKeys()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            request.RetireParent = true;
            request.ParentCertHashSignature = Sign(parent, request.SigningPayload());
            scope.ServiceProvider.GetRequiredService<CertificateController>().UpdatePublicCert(request);
            var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            var error = Assert.Throws<Npgsql.PostgresException>(() => db.GetService<IMigrator>()
                .Migrate("20260409170802_multiple_devices_support"));
            Assert.That(error.MessageText, Does.Contain("Cannot roll back certificate retirement"));
            Assert.That(db.Certificates.AsNoTracking().Single(x => x.Thumbprint == parent.Thumbprint).IsRetired, Is.True);
        }
    }

    [Test]
    public void LegacyPrivateCertificateColumnRejectsNewWrites()
    {
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
        db.Certificates.Add(new CertificateDb { Thumbprint = Guid.NewGuid().ToString(), PrivateCert = "test-only-forbidden-value" });
        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Throws<InvalidOperationException>(() => db.SaveChanges(false));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await db.SaveChangesAsync());
    }

    [Test]
    public void RetirementFlagCannotBeAddedToLegacyEnrollmentSignature()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            request.RetireParent = true;
            Assert.Throws<BadRequestException>(() => scope.ServiceProvider
                .GetRequiredService<CertificateController>().UpdatePublicCert(request));
            var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            Assert.That(db.Certificates.Single(x => x.Thumbprint == parent.Thumbprint).IsRetired, Is.False);
            Assert.That(db.Certificates.Any(x => x.ParentCertId == parent.Thumbprint), Is.False);
        }
    }

    [Test]
    public void LostSuccessResponseCanBeRetriedWithoutAddingCertificateOrDevice()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        {
            using var firstScope = ServiceProvider.CreateScope();
            var first = firstScope.ServiceProvider.GetRequiredService<CertificateController>().UpdatePublicCert(request);
            Assert.That(first.PublicCert, Is.EqualTo(request.PublicCert));

            // Simulate process restart after the server committed but before local metadata changed.
            using var retryScope = ServiceProvider.CreateScope();
            var retry = retryScope.ServiceProvider.GetRequiredService<CertificateController>().UpdatePublicCert(request);
            Assert.That(retry.PublicCert, Is.EqualTo(request.PublicCert));

            var db = retryScope.ServiceProvider.GetRequiredService<PassiDbContext>();
            Assert.That(db.Certificates.Count(x => x.ParentCertId == parent.Thumbprint), Is.EqualTo(1));
            Assert.That(db.Certificates.Where(x => x.ParentCertId == parent.Thumbprint).Select(x => x.PrivateCert).Single(), Is.Null);
            Assert.That(db.Certificates.Single(x => x.Thumbprint == parent.Thumbprint).IsRetired, Is.False);

            // DeviceId was not signed in the legacy contract; a replay must not mutate device links.
            var untrustedDeviceId = Guid.NewGuid().ToString();
            request.DeviceId = untrustedDeviceId;
            retryScope.ServiceProvider.GetRequiredService<CertificateController>().UpdatePublicCert(request);
            Assert.That(db.Devices.Any(x => x.DeviceId == untrustedDeviceId), Is.False);
        }
    }

    [Test]
    public void FailedSignatureCannotCreateOrAcknowledgeRotation()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            var controller = scope.ServiceProvider.GetRequiredService<CertificateController>();
            var validSignature = request.ParentCertHashSignature;
            request.ParentCertHashSignature = Convert.ToBase64String(new byte[256]);
            Assert.Throws<BadRequestException>(() => controller.UpdatePublicCert(request));
            var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            Assert.That(db.Certificates.Any(x => x.ParentCertId == parent.Thumbprint), Is.False);

            request.ParentCertHashSignature = validSignature;
            controller.UpdatePublicCert(request);
            request.ParentCertHashSignature = Convert.ToBase64String(new byte[256]);
            Assert.Throws<BadRequestException>(() => controller.UpdatePublicCert(request));
        }
    }

    [Test]
    public void RollbackBeforeCommitLeavesOldCertificateAndAllowsExactRetry()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        {
            request.RetireParent = true;
            request.ParentCertHashSignature = Sign(parent, request.SigningPayload());
            using (var failedScope = ServiceProvider.CreateScope())
            {
                var repo = failedScope.ServiceProvider.GetRequiredService<ICertificateRepository>();
                repo.GetExecutionStrategy().Execute(() =>
                {
                    using var transaction = repo.BeginTransaction();
                    failedScope.ServiceProvider.GetRequiredService<ICertificatesService>().UpdateCertificate(request);
                    transaction.Rollback();
                });
            }

            using var retryScope = ServiceProvider.CreateScope();
            var db = retryScope.ServiceProvider.GetRequiredService<PassiDbContext>();
            Assert.That(db.Certificates.Any(x => x.Thumbprint == parent.Thumbprint && !x.IsRetired), Is.True);
            Assert.That(db.Certificates.Any(x => x.ParentCertId == parent.Thumbprint), Is.False);
            var result = retryScope.ServiceProvider.GetRequiredService<CertificateController>().UpdatePublicCert(request);
            Assert.That(result.PublicCert, Is.EqualTo(request.PublicCert));
        }
    }

    [Test]
    public void DifferentSuccessorCannotReplaceAlreadyAcceptedRotation()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            var controller = scope.ServiceProvider.GetRequiredService<CertificateController>();
            controller.UpdatePublicCert(request);
            using var another = CreateCertificate(parent.GetNameInfo(X509NameType.SimpleName, true));
            request.PublicCert = Convert.ToBase64String(another.RawData);
            request.ParentCertHashSignature = Sign(parent, request.PublicCert);
            Assert.Throws<ClientException>(() => controller.UpdatePublicCert(request));
        }
    }

    [Test]
    public void RetryCannotReactivateAnAlreadyRetiredSuccessor()
    {
        var (parent, request) = CreateRotation();
        using (parent)
        using (var scope = ServiceProvider.CreateScope())
        {
            var controller = scope.ServiceProvider.GetRequiredService<CertificateController>();
            controller.UpdatePublicCert(request);
            var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            var successor = db.Certificates.Single(x => x.ParentCertId == parent.Thumbprint);
            successor.IsRetired = true;
            db.SaveChanges();
            Assert.Throws<ClientException>(() => controller.UpdatePublicCert(request));
            Assert.That(db.Certificates.AsNoTracking().Single(x => x.Thumbprint == successor.Thumbprint).IsRetired, Is.True);
        }
    }

    private (X509Certificate2 Parent, CertificateUpdateDto Request) CreateRotation()
    {
        using var scope = ServiceProvider.CreateScope();
        var signup = scope.ServiceProvider.GetRequiredService<SignUpController>();
        var email = $"{Guid.NewGuid()}@passi.cloud";
        var accountGuid = Guid.NewGuid();
        var deviceId = Guid.NewGuid().ToString();
        signup.SignUp(new SignupDto { Email = email, UserGuid = accountGuid, DeviceId = deviceId });
        var parent = CreateCertificate(email.Replace("@", ""));
        signup.Confirm(new SignupConfirmationDto
        {
            Code = scope.ServiceProvider.GetRequiredService<IUserRepository>().GetCode(email), DeviceId = deviceId, Email = email,
            Guid = accountGuid.ToString(), PublicCert = Convert.ToBase64String(parent.RawData)
        });
        using var next = CreateCertificate(email.Replace("@", ""));
        var publicCert = Convert.ToBase64String(next.RawData);
        return (parent, new CertificateUpdateDto
        {
            PublicCert = publicCert, ParentCertThumbprint = parent.Thumbprint,
            ParentCertHashSignature = Sign(parent, publicCert), DeviceId = deviceId,
        });
    }

    private static string Sign(X509Certificate2 certificate, string value)
    {
        using var key = certificate.GetRSAPrivateKey();
        return Convert.ToBase64String(key.SignData(Encoding.ASCII.GetBytes(value), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1));
    }

    private static X509Certificate2 CreateCertificate(string commonName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={commonName}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }
}
