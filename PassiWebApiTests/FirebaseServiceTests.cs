using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfigurationManager;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using NotificationsService;
using NUnit.Framework;
using RedisClient;

namespace PassiWebApiTests
{
    public class FirebaseServiceTests
    {
        private static AppSetting SessionTimeoutSetting(int minutes) =>
            new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["Timeout"] = minutes.ToString() })
                .Build())
            { PrefferAppsettingFile = true };

        [Test]
        public void LoginPushIsDataOnlyAndHighPrioritySoTheAppAlwaysHandlesIt()
        {
            var client = new CapturingFireBaseClient();
            var service = new FirebaseService(client, null, SessionTimeoutSetting(2), new CapturingLogger<FirebaseService>());

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
            var service = new FirebaseService(new ThrowingFireBaseClient(), redis, SessionTimeoutSetting(2), new CapturingLogger<FirebaseService>());

            service.SendNotification("device-token", "Passi login", "{\"SessionId\":\"abc\"}", "passi.cloud", Guid.NewGuid());

            Assert.That(redis.GetCalled.Wait(TimeSpan.FromSeconds(10)), Is.True, "error handler never ran");
        }

        [Test]
        public void SendNotificationOnPushFailureKeepsTheSessionsConfiguredTimeoutInsteadOfAHardcodedFiveMinutes()
        {
            // The error handler re-saves the session record so the error surfaces to the polling client.
            // That save must keep using the app's configured session Timeout - the same TTL the session
            // was created with - and not silently fall back to whatever default the underlying Redis
            // client happens to use for a save with no explicit expiry.
            const int configuredTimeoutMinutes = 45; // deliberately far from any hardcoded default
            var redis = new RecordingRedisService(new SessionTempRecord { Guid = Guid.NewGuid() });
            var service = new FirebaseService(new ThrowingFireBaseClient(), redis, SessionTimeoutSetting(configuredTimeoutMinutes), new CapturingLogger<FirebaseService>());

            service.SendNotification("device-token", "Passi login", "{\"SessionId\":\"abc\"}", "passi.cloud", Guid.NewGuid());

            Assert.That(redis.SaveCalled.Wait(TimeSpan.FromSeconds(10)), Is.True, "error handler never re-saved the session");
            Assert.That(redis.LastExpiry, Is.EqualTo(TimeSpan.FromMinutes(configuredTimeoutMinutes)));
        }

        [Test]
        public void SendNotificationLogsWhenRedisThrowsWhileHandlingAPushFailure()
        {
            // The error handler itself can fail: if Redis (already the fragile piece per this repo's
            // own notes) is unavailable while recording the push failure, that second exception must
            // not vanish as an unobserved exception on the background Task with no trace anywhere -
            // it should be logged.
            var logger = new CapturingLogger<FirebaseService>();
            var service = new FirebaseService(new ThrowingFireBaseClient(), new ThrowingGetRedisService(), SessionTimeoutSetting(2), logger);

            service.SendNotification("device-token", "Passi login", "{\"SessionId\":\"abc\"}", "passi.cloud", Guid.NewGuid());

            Assert.That(logger.LoggedError.Wait(TimeSpan.FromSeconds(10)), Is.True,
                "a Redis failure while handling a push failure was never logged");
            Assert.That(logger.LoggedException, Is.Not.Null);
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

        private class ThrowingGetRedisService : IRedisService
        {
            public void Add<T>(string key, T item, TimeSpan expire) { }

            public void Add<T>(string key, T item) { }

            public T Get<T>(string key) => throw new InvalidOperationException("Redis unavailable");

            public void Delete<T>(string key) { }

            public T GetAndDelete<T>(string key) => default;
        }

        private class CapturingLogger<T> : ILogger<T>
        {
            public readonly ManualResetEventSlim LoggedError = new(false);
            public Exception LoggedException;

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (logLevel < LogLevel.Error)
                    return;

                LoggedException = exception;
                LoggedError.Set();
            }
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

            public T GetAndDelete<T>(string key) => default;
        }

        private class RecordingRedisService : IRedisService
        {
            private readonly SessionTempRecord _session;
            public readonly ManualResetEventSlim SaveCalled = new(false);
            public TimeSpan? LastExpiry;

            public RecordingRedisService(SessionTempRecord session) => _session = session;

            public void Add<T>(string key, T item, TimeSpan expire)
            {
                LastExpiry = expire;
                SaveCalled.Set();
            }

            public void Add<T>(string key, T item)
            {
                LastExpiry = null; // no explicit expiry given - marks the bug this test guards against
                SaveCalled.Set();
            }

            public T Get<T>(string key) => (T)(object)_session;

            public void Delete<T>(string key) { }

            public T GetAndDelete<T>(string key) => default;
        }
    }
}
