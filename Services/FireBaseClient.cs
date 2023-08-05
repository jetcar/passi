using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using PostSharp.Extensibility;
using Message = FirebaseAdmin.Messaging.Message;

namespace Services
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class FireBaseClient : IFireBaseClient
    {
        public FireBaseClient()
        {
            var filePath = "/home/creds/google-services.json";
            var credential = GoogleCredential.FromFile(filePath);
            FirebaseApp.Create(new AppOptions()
            {
                Credential = credential
            });
        }

        public string Send(Message message)
        {
            return FirebaseMessaging.DefaultInstance.SendAsync(message).Result;
        }
    }

    public interface IFireBaseClient
    {
        string Send(Message message);
    }
}