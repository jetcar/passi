using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class FireBaseClient : IFireBaseClient
    {
        public FireBaseClient()
        {
            var filePath = "google-services.json";
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