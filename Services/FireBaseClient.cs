using ConfigurationManager;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using PostSharp.Extensibility;
using Message = FirebaseAdmin.Messaging.Message;

namespace Services
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class FireBaseClient : IFireBaseClient
    {
        public ILogger<FireBaseClient> _logger;
        private AppSetting _appSetting;

        public FireBaseClient(ILogger<FireBaseClient> logger, AppSetting appSetting)
        {
            _logger = logger;
            _appSetting = appSetting;
            var filePath = _appSetting["google-services-json-path"];
            var credential = GoogleCredential.FromFile(filePath);
            FirebaseApp.Create(new AppOptions()
            {
                Credential = credential
            });
        }

        public string Send(Message message)
        {
            if (string.IsNullOrEmpty(message.Token))
            {
                _logger.LogDebug("token is missing");
                return null;
            }
            return FirebaseMessaging.DefaultInstance.SendAsync(message).Result;
        }
    }

    public interface IFireBaseClient
    {
        string Send(Message message);
    }
}