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
    }
}
