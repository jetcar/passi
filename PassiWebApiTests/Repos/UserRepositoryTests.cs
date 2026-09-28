using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Repos;

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
    }
}
