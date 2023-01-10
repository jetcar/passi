using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Runtime;
using Android.OS;
using Android.Support.V4.Hardware.Fingerprint;
using AppCommon;
using AppConfig;
using Firebase.Messaging;
using Java.Lang;
using Newtonsoft.Json;
using passi_android.Droid.FingerPrint;
using passi_android.Droid.Notifications;
using passi_android.Notifications;
using passi_android.utils;
using RestSharp;
using WebApiDto;
using WebApiDto.Auth;
using Xamarin.Essentials;
using Android.Gms.Extensions;
using Xamarin.Forms;
using Java.Security;

namespace passi_android.Droid
{
    [Activity(Label = "Passi", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, DirectBootAware = true, Exported = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        internal static readonly string CHANNEL_ID = "passi_notification_channel_id";
        internal static readonly int NOTIFICATION_ID = 0;
        private string msgText = "";

        public static App App;

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

            var intent = Intent;
            //PackageManager.getla
            App = new App();
            //DateTimeService.Init();
            LoadApplication(App);
            var accounts = new ObservableCollection<Account>();
            MainPage.Providers = SecureRepository.LoadProvidersIntoList(accounts);

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, savedInstanceState);
            Task.Run(() =>
            {
                SecureRepository.GetDeviceId();

                var task = FirebaseMessaging.Instance.GetToken().GetAwaiter().GetResult();

                var token = task.ToString();

                MyFirebaseIIDService.SendRegistrationToServer(token);
            });

            //PollNotifications();
            App.PollNotifications = PollNotifications;
            App.CloseApp = () =>
            {
                this.FinishAffinity();
            };
            // Using API level 23:
            Task.Run(() =>
            {
                var fingerprintManagerCompat = FingerprintManagerCompat.From(this);

                App.FingerprintManager.HasEnrolledFingerprints = fingerprintManagerCompat.HasEnrolledFingerprints;
                App.FingerprintManager.IsHardwareDetected = fingerprintManagerCompat.IsHardwareDetected;
                App.IsKeyguardSecure = ((KeyguardManager)GetSystemService(KeyguardService)).IsKeyguardSecure;
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
            PollNotifications();

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

        private static object locker = new object();

        public static void PollNotifications()
        {
            lock (locker)
            {
                PollingTask ??= Task.Run(() =>
                {
                    var accounts = new ObservableCollection<Account>();
                    SecureRepository.LoadAccountIntoList(accounts);
                    SecureRepository.LoadProvidersIntoList(accounts);
                    var groupedAccounts = accounts.GroupBy(x => x.ProviderName);
                    foreach (var groupedAccount in groupedAccounts)
                    {
                        var provider = groupedAccount.ToList().First().Provider ?? MainPage.Providers.First(x=>x.IsDefault);

                        var getAllSessionDto = new GetAllSessionDto()
                        {
                            DeviceId = SecureRepository.GetDeviceId()
                        };

                        var guids = accounts.Select(x => x.Guid.ToString()).ToList();
                        RestService.ExecutePostAsync(provider, provider.SyncAccounts, new SyncAccountsDto()
                        {
                            DeviceId = SecureRepository.GetDeviceId(),
                            Guids = guids
                        }
                        ).ContinueWith(
                            task =>
                            {
                                task.GetAwaiter().GetResult();
                                if (task.Result.IsSuccessful)
                                {
                                    var serverAccounts =
                                        JsonConvert.DeserializeObject<List<AccountMinDto>>(task.Result.Content);
                                    foreach (var account in accounts)
                                    {
                                        if (serverAccounts.All(x => x.UserGuid != account.Guid))
                                        {
                                            var loadedAccount = SecureRepository.GetAccount(account.Guid);
                                            if (loadedAccount != null && loadedAccount.IsConfirmed)
                                            {
                                                loadedAccount.Inactive = true;
                                                SecureRepository.UpdateAccount(loadedAccount);
                                            }
                                        }
                                    }

                                    if (App.AccountSyncCallback != null)
                                        App.AccountSyncCallback.Invoke();
                                }
                            });


                        var response = RestService
                            .ExecutePostAsync(provider, provider.CheckForStartedSessions, getAllSessionDto).Result;
                        if (response.IsSuccessful)
                        {
                            var msg = JsonConvert.DeserializeObject<NotificationDto>(response.Content);
                            if (msg != null)
                            {
                                if (SecureRepository.AddSessionKey(msg.SessionId))
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        NotificationVerifyRequestView.Instance.Message = msg;
                                        App.MainPage.Navigation.PushModalSinglePage(NotificationVerifyRequestView
                                            .Instance);
                                    });
                            }
                        }
                    }

                    PollingTask = null;
                });
            }
        }

        public static Task PollingTask { get; set; }

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
            channel.SetAllowBubbles(true);
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