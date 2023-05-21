using Android.App;
using Android.Content.PM;
using Android.Gms.Common;
using Android.OS;
using AndroidX.Core.Hardware.Fingerprint;
using Newtonsoft.Json;
using passi_maui.utils;
using Plugin.Firebase.CloudMessaging;
using System.Collections.ObjectModel;
using passi_maui.FingerPrint;
using passi_maui.Notifications;
using WebApiDto;
using WebApiDto.Auth;
using System.Security.Cryptography.X509Certificates;
using Android.Security.Keystore;
using AndroidX.Security.Crypto;
using Java.Security;
using Encoding = System.Text.Encoding;

namespace passi_maui.Platforms.Android
{
    [Activity(Label = "Passi", MainLauncher = true, Theme = "@style/MainTheme", DirectBootAware = true, Exported = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : MauiAppCompatActivity
    {
        private string msgText = "";
        internal static readonly string CHANNEL_ID = "passi_notification_channel_id";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            App.PollNotifications = PollNotifications;
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
                        var tokenres = token.Result;
                        MyFirebaseService.SendRegistrationToServer(tokenres);
                    });
                });


            });
            App.CloseApp = () =>
            {
                this.FinishAffinity();
            };
            App.CreateCertificate = () =>
            {
                return GenerateCertificate("test", "test").Result;
                
            };


            App.CreateCertificate.Invoke();

            //Task.Run(() =>
            //{
            //    var fingerprintManagerCompat = FingerprintManagerCompat.From(this);

            //    App.FingerprintManager.HasEnrolledFingerprints = () =>
            //    {
            //        return fingerprintManagerCompat.HasEnrolledFingerprints;
            //    };
            //    App.FingerprintManager.IsHardwareDetected = () =>
            //    {
            //        return fingerprintManagerCompat.IsHardwareDetected;
            //    };
            //    App.IsKeyguardSecure = () =>
            //    {
            //        return ((KeyguardManager)GetSystemService(KeyguardService)).IsKeyguardSecure;
            //    };
            //    App.StartFingerPrintReading = FingerPrintAuthentication;
            //});

            App.CancelNotifications = () =>
            {
                var notificationManager =
                    (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CancelAll();
            };

        }

        protected override void OnPostResume()
        {
            PollNotifications();

            base.OnPostResume();
        }


        public async Task<Tuple<X509Certificate2, string, byte[]>> GenerateCertificate(string subject, string pin)
        {
            var mainKey = new MasterKey.Builder(this.ApplicationContext).SetKeyScheme(MasterKey.KeyScheme.Aes256Gcm)
                .Build();
            KeyPairGenerator kpg =  KeyPairGenerator.GetInstance(
                KeyProperties.KeyAlgorithmRsa, "AndroidKeyStore");
            kpg.Initialize(new KeyGenParameterSpec.Builder(
                    subject,KeyStorePurpose.Sign| KeyStorePurpose.Verify)
                .SetDigests(KeyProperties.DigestSha256,
                    KeyProperties.DigestSha512).Build());

            KeyPair kp = kpg.GenerateKeyPair();
            var bytes = kp.Public.GetEncoded();

            var cert = Encoding.UTF8.GetString(bytes);
            return null;
        }



        protected void FingerPrintAuthentication()
        {
            const int flags = 0; /* always zero (0) */

            // CryptoObjectHelper is described in the previous section.
            CryptoObjectHelper cryptoHelper = new CryptoObjectHelper();

            // cancellationSignal can be used to manually stop the fingerprint scanner.
            var cancellationSignal = new CancellationSignal();
            App.CancelfingerPrint = () =>
            {
                cancellationSignal.Cancel();
            };
            FingerprintManagerCompat fingerprintManager = FingerprintManagerCompat.From(this);

            // AuthenticationCallback is a base class that will be covered later on in this guide.
            FingerprintManagerCompat.AuthenticationCallback authenticationCallback = new MyAuthCallbackSample(this);
            // Start the fingerprint scanner.
            //fingerprintManager.Authenticate(cryptoHelper.BuildCryptoObject(), flags, cancellationSignal, authenticationCallback, null);
        }

        private static object locker = new object();

        public static void PollNotifications()
        {
            PollingTask ??= Task.Run(() =>
            {
                lock (locker)
                {

                    var accounts = new ObservableCollection<utils.AccountView>();
                    SecureRepository.LoadAccountIntoList(accounts);
                    var providers = SecureRepository.LoadProviders();
                    var groupedAccounts = accounts.GroupBy(x => x.ProviderGuid);
                    foreach (var groupedAccount in groupedAccounts)
                    {
                        var providerGuid = groupedAccount.ToList().First().ProviderGuid ?? providers.First(x => x.IsDefault).Guid;
                        var provider = SecureRepository.LoadProviders().First(x => x.Guid == providerGuid);
                        var getAllSessionDto = new GetAllSessionDto()
                        {
                            DeviceId = SecureRepository.GetDeviceId()
                        };

                        var guids = accounts.Select(x => x.Guid.ToString()).ToList();
                        var task = RestService.ExecutePostAsync(provider, provider.SyncAccounts, new SyncAccountsDto()
                        {
                            DeviceId = SecureRepository.GetDeviceId(),
                            Guids = guids
                        });

                        var restResponse = task.Result;
                        if (restResponse.IsSuccessful)
                        {
                            var serverAccounts = JsonConvert.DeserializeObject<List<AccountMinDto>>(restResponse.Content);
                            foreach (var account in groupedAccount)
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



                        var task2 = RestService.ExecutePostAsync(provider, provider.CheckForStartedSessions, getAllSessionDto);

                        var response = task2.Result;
                        if (response.IsSuccessful)
                        {
                            var msg = JsonConvert.DeserializeObject<NotificationDto>(response.Content);
                            if (msg != null)
                            {
                                if (SecureRepository.CheckSessionKey(msg.SessionId))
                                {
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        AppShell.MainPage.Navigation.PushModalSinglePage(new NotificationVerifyRequestView(), new Dictionary<string, object>() { { "Message", msg } });
                                    });
                                }
                            }
                        }

                    }

                    PollingTask = null;
                }

            });
        }

        public static Task PollingTask { get; set; }

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