using Android.App;
using Android.Util;
using Firebase.Messaging;
using Microsoft.Extensions.DependencyInjection;
using passi_android.utils;
using passi_android.utils.Services;
using System.Threading;
using WebApiDto;

namespace passi_android.Droid.Notifications
{
    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class MyFirebaseIIDService : FirebaseMessagingService
    {
        public override void OnNewToken(string p0)
        {
            var refreshedToken = FirebaseMessaging.Instance.GetToken().Result.ToString();
            Log.Debug("MyFirebaseIIDService", "Refreshed token: " + refreshedToken);
            SendRegistrationToServer(refreshedToken);
            base.OnNewToken(p0);
        }

        public static void SendRegistrationToServer(string token)
        {
            var secureRepository = App.Services.GetService<ISecureRepository>();
            var restService = App.Services.GetService<IRestService>();
            var mySecureStorage = App.Services.GetService<IMySecureStorage>();

            var deviceTokenUpdateDto = new DeviceTokenUpdateDto() { DeviceId = secureRepository.GetDeviceId(), Token = token, Platform = Plugin.DeviceInfo.CrossDeviceInfo.Current.Platform.ToString() };
            foreach (var provider in secureRepository.LoadProviders())
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    var result = restService.ExecutePostAsync(provider, provider.TokenUpdate, deviceTokenUpdateDto)
                        .Result;
                    if (!result.IsSuccessful)
                    {
                        do
                        {
                            Thread.Sleep(10000);
                            result = restService.ExecutePostAsync(provider, provider.TokenUpdate, deviceTokenUpdateDto)
                                .Result;
                        } while (!result.IsSuccessful);

                        mySecureStorage.SetAsync(StorageKeys.NotificationToken, token);
                    }
                    else
                    {
                        mySecureStorage.SetAsync(StorageKeys.NotificationToken, token);
                    }
                });
            }
        }
    }
}