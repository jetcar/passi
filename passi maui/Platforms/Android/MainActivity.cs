﻿using Android.App;
using Android.Content.PM;
using Android.Gms.Common;
using Android.OS;
using passi_maui.utils;
using Plugin.Firebase.CloudMessaging;

namespace passi_maui.Platforms.Android
{
    [Activity(Label = "Passi", MainLauncher = true, Theme = "@style/MainTheme", DirectBootAware = true, Exported = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : MauiAppCompatActivity
    {
        private string msgText = "";
        internal static readonly string CHANNEL_ID = "passi_notification_channel_id";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            IsPlayServicesAvailable();
            CreateNotificationChannel();
            Task.Run(() =>
            {
                SecureRepository.GetDeviceId();
                CrossFirebaseCloudMessaging.Current.CheckIfValidAsync().ContinueWith((a) =>
                {
                    CrossFirebaseCloudMessaging.Current.GetTokenAsync().ContinueWith((token) =>
                    {
                        MyFirebaseService.SendRegistrationToServer(token.Result);
                    });
                });
               

            });

        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            var channel = new NotificationChannel(CHANNEL_ID,
                "FCM Notifications",
                NotificationImportance.Default)
            {
                Description = "Firebase Cloud Messages appear in this channel",
                LockscreenVisibility = NotificationVisibility.Public,
                Importance = NotificationImportance.High
            };
            //channel.SetAllowBubbles(true);
            var notificationManager = (NotificationManager)GetSystemService(global::Android.Content.Context.NotificationService);
            notificationManager.CreateNotificationChannel(channel);
            FirebaseCloudMessagingImplementation.ChannelId = CHANNEL_ID;

        }


        public bool IsPlayServicesAvailable()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (resultCode != ConnectionResult.Success)
            {
                if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
                    msgText = GoogleApiAvailability.Instance.GetErrorString(resultCode);
                else
                {
                    msgText = "This device is not supported";
                    Finish();
                }
                return false;
            }
            else
            {
                msgText = "Google Play Services is available.";
                return true;
            }
        }
    }
}