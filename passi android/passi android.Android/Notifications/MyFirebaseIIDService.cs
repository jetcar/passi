﻿using System.Threading;
using Android.App;
using Android.Util;
using Firebase.Messaging;
using Microsoft.Extensions.DependencyInjection;
using passi_android.utils;
using WebApiDto;
using Xamarin.Essentials;

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
            var _secureRepository = App.Services.GetService<ISecureRepository>();
            var _restService = App.Services.GetService<IRestService>();
            var _mySecureStorage = App.Services.GetService<IMySecureStorage>();

            var deviceTokenUpdateDto = new DeviceTokenUpdateDto() { DeviceId = _secureRepository.GetDeviceId(), Token = token, Platform = Plugin.DeviceInfo.CrossDeviceInfo.Current.Platform.ToString() };
            foreach (var provider in _secureRepository.LoadProviders())
            {
                var result = _restService.ExecutePostAsync(provider, provider.TokenUpdate, deviceTokenUpdateDto);
                if (!result.Result.IsSuccessful)
                {
                    do
                    {
                        Thread.Sleep(10000);
                        result = _restService.ExecutePostAsync(provider, provider.TokenUpdate, deviceTokenUpdateDto);
                    } while (!result.Result.IsSuccessful);

                    _mySecureStorage.SetAsync(StorageKeys.NotificationToken, token);
                }
                else
                {
                    _mySecureStorage.SetAsync(StorageKeys.NotificationToken, token);
                }
            }
        }
    }
}