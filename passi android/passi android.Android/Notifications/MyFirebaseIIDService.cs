using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Util;
using AppConfig;
using Firebase.Messaging;
using passi_android.utils;
using RestSharp;
using WebApiDto;
using Xamarin.Essentials;

namespace passi_android.Droid.Notifications
{
    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class MyFirebaseIIDService : FirebaseMessagingService
    {
        public MyFirebaseIIDService()
        {
        }

        public override void OnNewToken(string p0)
        {
            var refreshedToken = FirebaseMessaging.Instance.GetToken().Result.ToString();
            Log.Debug("MyFirebaseIIDService", "Refreshed token: " + refreshedToken);
            SendRegistrationToServer(refreshedToken);
            base.OnNewToken(p0);
        }

        public static void SendRegistrationToServer(string token)
        {
            var deviceTokenUpdateDto = new DeviceTokenUpdateDto() { DeviceId = SecureRepository.GetDeviceId(), Token = token, Platform = Plugin.DeviceInfo.CrossDeviceInfo.Current.Platform.ToString() };
            var result = RestService.ExecutePostAsync(ConfigSettings.TokenUpdate, deviceTokenUpdateDto);
            if (!result.Result.IsSuccessful)
            {
                do
                {
                    Thread.Sleep(10000);
                    result = RestService.ExecutePostAsync(ConfigSettings.TokenUpdate, deviceTokenUpdateDto);
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