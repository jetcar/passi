using System;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using Models;
using NotificationsService;
using NUnit.Framework;
using RedisClient;

namespace PassiWebApiTests
{
    public class FirebaseServiceTests
    {
        [Test]
        public void LoginPushIsDataOnlyAndHighPrioritySoTheAppAlwaysHandlesIt()
        {
            var client = new CapturingFireBaseClient();
            var service = new FirebaseService(client, null);

            service.SendNotification("device-token", "Passi login", "{\"SessionId\":\"abc\"}", "passi.cloud", Guid.NewGuid());

            Assert.That(client.Sent.Wait(TimeSpan.FromSeconds(10)), Is.True, "push was not sent");
            var message = client.Message;
            // A notification block makes Android display the push itself while the app is in the background,
            // bypassing the app's handler (no full-screen intent, wrong channel). Data-only lets the app handle it.
            Assert.That(message.Notification, Is.Null);
            Assert.That(message.Token, Is.EqualTo("device-token"));
            Assert.That(message.Data["title"], Is.EqualTo("Passi login"));
            Assert.That(message.Data["body"], Is.EqualTo("{\"SessionId\":\"abc\"}"));
            Assert.That(message.Android?.Priority, Is.EqualTo(Priority.High));
        }

        [Test]
        public void SendNotificationSurvivesAPushFailureForASessionThatAlreadyExpired()
        {
            // The error handler runs ~3s later on its own background Thread. If the session already
            // expired out of Redis by then, dereferencing the (null) lookup result would throw a
            // NullReferenceException on that raw Thread, which is unhandled and terminates the whole
            // process. Simulate that race: the push fails, and Redis no longer has the session.
            var redis = new NullReturningRedisService();
            var service = new FirebaseService(new ThrowingFireBaseClient(), redis);

            service.SendNotification("device-token", "Passi login", "{\"SessionId\":\"abc\"}", "passi.cloud", Guid.NewGuid());

            Assert.That(redis.GetCalled.Wait(TimeSpan.FromSeconds(10)), Is.True, "error handler never ran");
        }

        private class CapturingFireBaseClient : IFireBaseClient
        {
            public readonly ManualResetEventSlim Sent = new(false);
            public Message Message;

            public Task<string> SendAsync(Message message)
            {
                Message = message;
                Sent.Set();
                return Task.FromResult("id");
            }
        }

        private class ThrowingFireBaseClient : IFireBaseClient
        {
            public Task<string> SendAsync(Message message) => Task.FromException<string>(new InvalidOperationException("FCM rejected the token"));
        }

        private class NullReturningRedisService : IRedisService
        {
            public readonly ManualResetEventSlim GetCalled = new(false);

            public void Add<T>(string key, T item, TimeSpan expire) { }

            public void Add<T>(string key, T item) { }

            public T Get<T>(string key)
            {
                GetCalled.Set();
                return default;
            }

            public void Delete<T>(string key) { }
        }
    }
}
