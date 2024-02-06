using Android.App;
using Android.Content.PM;
using Android.OS;
using AppCommon;
using MauiApp2.utils.Services;
using MauiViewModels.FingerPrint;
using MauiViewModels.utils.Services;
using MauiViewModels.utils.Services.Certificate;
using IMainThreadService = MauiApp2.utils.Services.IMainThreadService;

namespace MauiApp2.Platforms.Android
{
    [Activity(Label = "Passi", Icon = "@mipmap/appicon", Theme = "@style/Maui.SplashTheme", MainLauncher = true, DirectBootAware = true, Exported = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : MauiAppCompatActivity
    {
        internal static readonly string CHANNEL_ID = "passi_notification_channel_id";

        public static MainActivity Instance;

        public MainActivity()
        {
            App.Services = ConfigureServices();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            IsPlayServicesAvailable();

            CreateNotificationChannel();
            var secureRepository = App.Services.GetService<ISecureRepository>();
            var dateTimeService = App.Services.GetService<IDateTimeService>();
            dateTimeService.Init();

            Task.Run(() =>
            {
                secureRepository.GetDeviceId();

                //var task = FirebaseMessaging.Instance.GetToken().GetAwaiter().GetResult();

                //var token = task.ToString();

                // MyFirebaseIIDService.SendRegistrationToServer(token);
            });

            App.CloseApp = () =>
            {
                this.FinishAffinity();
            };
            App.StartFingerPrintReading = FingerPrintAuthentication;

            App.CancelNotifications = () =>
            {
                var notificationManager =
                    (NotificationManager)GetSystemService(Android.MainActivity.NotificationService);
                notificationManager.CancelAll();
            };
        }

        protected void FingerPrintAuthentication()
        {
            var biometricHelper = App.Services.GetService<IBiometricHelper>();
            biometricHelper.RegisterOrAuthenticate();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ISecureRepository, SecureRepository>();
            services.AddSingleton<IDateTimeService, DateTimeService>();
            services.AddSingleton<ICertificatesService, CertificatesService>();
            services.AddSingleton<ICertHelper, CertHelper>();
            services.AddSingleton<ISyncService, SyncService>();
            services.AddSingleton<IMySecureStorage, MySecureStorage>();
            services.AddSingleton<IRestService, RestService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IMainThreadService, MainThreadService>();
            services.AddSingleton<IFingerPrintService, FingerPrintService>();

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

        public bool IsPlayServicesAvailable()
        {
            //var msgText = "";
            //int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            //if (resultCode != ConnectionResult.Success)
            //{
            //    if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
            //        msgText = GoogleApiAvailability.Instance.GetErrorString(resultCode);
            //    else
            //    {
            //        msgText = "This device is not supported";
            //        Finish();
            //    }
            //    return false;
            //}
            //else
            //{
            //    msgText = "Google Play Services is available.";
            return true;
            //}
        }
    }
}