using Android.App;
using Android.Util;
using Firebase.Messaging;
using passi_maui.utils;
using WebApiDto;

namespace passi_maui.Platforms.Android
{
    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]

    internal class MyFirebaseService : FirebaseMessagingService
    {
        public override void OnNewToken(string token)
        {

            var refreshedToken = FirebaseMessaging.Instance.GetToken().Result.ToString();
            Log.Debug("MyFirebaseIIDService", "Refreshed token: " + refreshedToken);
            SendRegistrationToServer(refreshedToken);

            base.OnNewToken(token);
        }

        public static void SendRegistrationToServer(string token)
        {
            var deviceTokenUpdateDto = new DeviceTokenUpdateDto() { DeviceId = SecureRepository.GetDeviceId(), Token = token, Platform = "Android" };
            foreach (var provider in SecureRepository.LoadProviders())
            {
                var result = RestService.ExecutePostAsync(provider, provider.TokenUpdate, deviceTokenUpdateDto);
                if (!result.Result.IsSuccessful)
                {
                    do
                    {
                        Thread.Sleep(10000);
                        result = RestService.ExecutePostAsync(provider, provider.TokenUpdate, deviceTokenUpdateDto);
                    } while (!result.Result.IsSuccessful);

                    SecureStorage.SetAsync(StorageKeys.NotificationToken, token);
                }
                else
                {
                    SecureStorage.SetAsync(StorageKeys.NotificationToken, token);
                }
            }
        }
    }
}
