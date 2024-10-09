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

        public FirebaseService(IFireBaseClient fireBaseClient, IRedisService redisService)
        {
            _fireBaseClient = fireBaseClient;
            _redisService = redisService;
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
                Notification = new Notification()
                {
                    Title = title,
                    Body = notification,
                },
                //Android = new AndroidConfig() { TimeToLive = new TimeSpan(0, 1, 0) }
            };
            new Thread(() =>
            {
                Thread.Sleep(3000);
                try
                {
                    var response = _fireBaseClient.Send(message);
                }
                catch (Exception e)
                {
                    var session = _redisService.Get<SessionTempRecord>(sessionId.ToString());

                    session.Status = Models.SessionStatus.Error;
                    session.ErrorMessage = Truncate(e.Message, 256);
                    _redisService.Add(sessionId.ToString(), session);

                }
            }).Start();
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