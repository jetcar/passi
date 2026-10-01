using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigurationManager;
using FirebaseAdmin.Messaging;
using GoogleTracer;
using Models;
using RedisClient;
using Message = FirebaseAdmin.Messaging.Message;

namespace NotificationsService
{
    [Profile]
    public class FirebaseService : IFirebaseService
    {

        private IFireBaseClient _fireBaseClient;
        private IRedisService _redisService;
        private readonly int _sessionTimeout;

        public FirebaseService(IFireBaseClient fireBaseClient, IRedisService redisService, AppSetting appSetting)
        {
            _fireBaseClient = fireBaseClient;
            _redisService = redisService;
            _sessionTimeout = Convert.ToInt32(appSetting["Timeout"]);
        }

        public virtual string SendNotification(string clientToken, string title, string body, string notification, Guid sessionId)
        {
            var registrationTokens = clientToken;
            var message = new Message()
            {
                Token = registrationTokens,
                Data = new Dictionary<string, string>()
                {
                    {"title", title},
                    {"body", body},
                },
                // Data-only on purpose: with a notification block, Android shows the push itself while the app is
                // in the background and the app's handler never runs (no full-screen intent, generic channel).
                Android = new AndroidConfig() { Priority = Priority.High },
            };
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                try
                {
                    var response = await _fireBaseClient.SendAsync(message);
                }
                catch (Exception e)
                {
                    var session = _redisService.Get<SessionTempRecord>(sessionId.ToString());
                    if (session == null)
                        return;

                    session.Status = Models.SessionStatus.Error;
                    session.ErrorMessage = Truncate(e.Message, 256);
                    _redisService.Add(sessionId.ToString(), session, TimeSpan.FromMinutes(_sessionTimeout));

                }
            });
            return "";
        }

        public static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // If we're asked for more than we've got, we can just return the
            // original reference
            return text.Length > maxLength ? text.Substring(0, maxLength) : text;
        }
    }

    public interface IFirebaseService
    {
        string SendNotification(string clientToken, string title, string body, string notification, Guid sessionId);
    }
}