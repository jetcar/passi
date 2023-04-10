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
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

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


        public static async Task<Tuple<X509Certificate2, string, byte[]>> GenerateCertificate(string subject, string pin)
        {
            var random = new SecureRandom();
            var certificateGenerator = new X509V3CertificateGenerator();

            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            var username = subject;
            certificateGenerator.SetIssuerDN(new X509Name($"C=NL, O=Passi, CN={username}"));
            certificateGenerator.SetSubjectDN(new X509Name($"C=NL, O=Passi, CN={username}"));
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(1));

            const int strength = 2048;
            var keyGenerationParameters = new KeyGenerationParameters(random, strength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);

            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            var issuerKeyPair = subjectKeyPair;
            const string signatureAlgorithm = "SHA512WithRSA";
            var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, issuerKeyPair.Private);
            var bouncyCert = certificateGenerator.Generate(signatureFactory);

            // Lets convert it to X509Certificate2

            Pkcs12Store store = new Pkcs12StoreBuilder().Build();
            store.SetKeyEntry($"{username}_key", new AsymmetricKeyEntry(subjectKeyPair.Private), new[] { new X509CertificateEntry(bouncyCert) });

            var password = Guid.NewGuid().ToString();
            var charArray = (password + pin).ToCharArray();
            var fullPassword = (charArray);

            using (var ms = new System.IO.MemoryStream())
            {
                store.Save(ms, fullPassword.ToArray(), random);
                var bytes = ms.ToArray();
                var certificate = new X509Certificate2(bytes, fullPassword, X509KeyStorageFlags.DefaultKeySet);
                var result = new Tuple<X509Certificate2, string, byte[]>(certificate, password, bytes);
                return result;
            }
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