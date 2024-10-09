using Android.App;
using Android.Content.PM;
using Android.Gms.Common;
using Android.OS;
using AppCommon;
using MauiApp2.FingerPrint;
using MauiApp2.utils.Services;
using MauiViewModels;
using MauiViewModels.FingerPrint;
using MauiViewModels.Notifications;
using MauiViewModels.utils.Services;
using MauiViewModels.utils.Services.Certificate;
using Plugin.Firebase.CloudMessaging;
using Timer = System.Timers.Timer;

namespace MauiApp2.Platforms.Android
{
    [Activity(Label = "Passi", Icon = "@mipmap/icon", Theme = "@style/Maui.SplashTheme", MainLauncher = true, DirectBootAware = true, Exported = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : MauiAppCompatActivity
    {
        internal static readonly string CHANNEL_ID = "passi_notification_channel_id";

        public static MainActivity Instance;
        private readonly Timer _timer;

        public MainActivity()
        {
            Instance = this;
            CommonApp.Services = ConfigureServices();

            _timer = new Timer();
            _timer.Enabled = true;
            _timer.Interval = 1000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var syncService = CommonApp.Services.GetService<ISyncService>();
            syncService.PollNotifications();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            IsPlayServicesAvailable();
            CommonApp.Version = AppInfo.Current.VersionString;
            CreateNotificationChannel();
            var secureRepository = CommonApp.Services.GetService<ISecureRepository>();
            var dateTimeService = CommonApp.Services.GetService<IDateTimeService>();

            Task.Run(() =>
            {
                dateTimeService.Init();
                secureRepository.GetDeviceId();
                CrossFirebaseCloudMessaging.Current.CheckIfValidAsync().GetAwaiter().GetResult();
                var token = CrossFirebaseCloudMessaging.Current.GetTokenAsync().Result;

                MyFirebaseIIDService.SendRegistrationToServer(token);
            });

            CommonApp.CloseApp = () =>
            {
                this.FinishAffinity();
            };
            CommonApp.StartFingerPrintReading = FingerPrintAuthentication;

            CommonApp.CancelNotifications = () =>
            {
                var notificationManager =
                    (NotificationManager)GetSystemService(Android.MainActivity.NotificationService);
                notificationManager.CancelAll();
            };
        }

        protected void FingerPrintAuthentication()
        {
            var biometricHelper = CommonApp.Services.GetService<IBiometricHelper>();
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
            services.AddSingleton<IBiometricHelper, BiometricHelper>();

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
            var msgText = "";
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