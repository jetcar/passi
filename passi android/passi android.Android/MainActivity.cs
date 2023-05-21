using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Runtime;
using Android.OS;
using Android.Support.V4.Hardware.Fingerprint;
using Firebase.Messaging;
using passi_android.Droid.FingerPrint;
using passi_android.Droid.Notifications;
using Xamarin.Essentials;
using Android.Gms.Extensions;
using AppCommon;
using Microsoft.Extensions.DependencyInjection;
using passi_android.utils.Services;
using passi_android.utils.Services.Certificate;

namespace passi_android.Droid
{
    [Activity(Label = "Passi", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, DirectBootAware = true, Exported = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        internal static readonly string CHANNEL_ID = "passi_notification_channel_id";
        internal static readonly int NOTIFICATION_ID = 0;
        private string msgText = "";

        public static App App;

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ISecureRepository, SecureRepository>();
            services.AddSingleton<IDateTimeService, DateTimeService>();
            services.AddSingleton<ICertConverter, CertConverter>();
            services.AddSingleton<ICertificatesService, CertificatesService>();
            services.AddSingleton<ICertHelper, CertHelper>();
            services.AddSingleton<ISyncService, SyncService>();
            services.AddSingleton<IMySecureStorage, MySecureStorage>();
            services.AddSingleton<IRestService, RestService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IMainThreadService, MainThreadService>();

            return services.BuildServiceProvider();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var context = Android.App.Application.Context;
            var appinfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);
            App.Version = appinfo.VersionName;
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            IsPlayServicesAvailable();

            CreateNotificationChannel();

            App.Services = ConfigureServices();


            App = new App();
            var secureRepository = App.Services.GetService<ISecureRepository>();
            var dateTimeService = App.Services.GetService<IDateTimeService>();
            dateTimeService.Init();
            LoadApplication(App);

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, savedInstanceState);
            Task.Run(() =>
            {
                secureRepository.GetDeviceId();

                var task = FirebaseMessaging.Instance.GetToken().GetAwaiter().GetResult();

                var token = task.ToString();

                MyFirebaseIIDService.SendRegistrationToServer(token);
            });

            App.CloseApp = () =>
            {
                this.FinishAffinity();
            };
            // Using API level 23:
            Task.Run(() =>
            {
                var fingerprintManagerCompat = FingerprintManagerCompat.From(this);

                App.FingerprintManager.HasEnrolledFingerprints = () =>
                {
                    return fingerprintManagerCompat.HasEnrolledFingerprints;
                };
                App.FingerprintManager.IsHardwareDetected = () =>
                {
                    return fingerprintManagerCompat.IsHardwareDetected;
                };
                App.IsKeyguardSecure = () =>
                {
                    return ((KeyguardManager)GetSystemService(KeyguardService)).IsKeyguardSecure;
                };
                App.StartFingerPrintReading = FingerPrintAuthentication;
            });

            App.CancelNotifications = () =>
            {
                var notificationManager =
                    (NotificationManager)GetSystemService(Android.Content.Context.NotificationService);
                notificationManager.CancelAll();
            };
        }

        //protected override void OnNewIntent(Intent intent)
        //{
        //    PollNotifications();

        //    base.OnNewIntent(intent);
        //}

        protected override void OnPostResume()
        {
            var _syncService = App.Services.GetService<ISyncService>();

            _syncService.PollNotifications();

            base.OnPostResume();
        }

        protected void FingerPrintAuthentication()
        {
            const int flags = 0; /* always zero (0) */

            // CryptoObjectHelper is described in the previous section.
            CryptoObjectHelper cryptoHelper = new CryptoObjectHelper();

            // cancellationSignal can be used to manually stop the fingerprint scanner.
            var cancellationSignal = new Android.Support.V4.OS.CancellationSignal();
            App.CancelfingerPrint = () =>
            {
                cancellationSignal.Cancel();
            };
            FingerprintManagerCompat fingerprintManager = FingerprintManagerCompat.From(this);

            // AuthenticationCallback is a base class that will be covered later on in this guide.
            FingerprintManagerCompat.AuthenticationCallback authenticationCallback = new MyAuthCallbackSample(this);
            // Start the fingerprint scanner.
            fingerprintManager.Authenticate(cryptoHelper.BuildCryptoObject(), flags, cancellationSignal, authenticationCallback, null);
        }



        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
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