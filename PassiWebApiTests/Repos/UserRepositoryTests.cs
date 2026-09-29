using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    }
}
