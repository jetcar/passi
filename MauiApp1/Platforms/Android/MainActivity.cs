using Android.App;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using AppCommon;
using MauiApp1.utils;
using MauiCommonServices;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace MauiApp1
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public static MainActivity Instance;
        internal static readonly string CHANNEL_ID = "passichat_notification_channel_id";

        public MainActivity()
        {
            Instance = this;
            CommonApp.Services = ConfigureServices();

        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //IsPlayServicesAvailable();
            CommonApp.Version = AppInfo.Current.VersionString;
            CreateNotificationChannel();
            var dateTimeService = CommonApp.Services.GetService<IDateTimeService>();

            Task.Run(() =>
            {
                dateTimeService.Init();
                //CrossFirebaseCloudMessaging.Current.CheckIfValidAsync().GetAwaiter().GetResult();
                //var token = CrossFirebaseCloudMessaging.Current.GetTokenAsync().Result;

            });

            CommonApp.CloseApp = () =>
            {
                this.FinishAffinity();
            };

            CommonApp.CancelNotifications = () =>
            {
                var notificationManager =
                    (NotificationManager)GetSystemService(MainActivity.NotificationService);
                notificationManager.CancelAll();
            };
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IDateTimeService, DateTimeService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IMainThreadService, MainThreadService>();

            return services.BuildServiceProvider();
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
        }

        //public bool IsPlayServicesAvailable()
        //{
        //    var msgText = "";
        //    int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
        //    if (resultCode != ConnectionResult.Success)
        //    {
        //        if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
        //            msgText = GoogleApiAvailability.Instance.GetErrorString(resultCode);
        //        else
        //        {
        //            msgText = "This device is not supported";
        //            Finish();
        //        }
        //        return false;
        //    }
        //    else
        //    {
        //        msgText = "Google Play Services is available.";
        //        return true;
        //    }
        //}
    }


}
