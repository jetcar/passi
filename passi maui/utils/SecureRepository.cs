using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using AppConfig;
using Newtonsoft.Json;
using CertHelper = passi_maui.utils.Certificate.CertHelper;

namespace passi_maui.utils
{
    public static class SecureRepository
    {
        private static object _locker = new object();
        public static List<ProviderDb> Providers { get; set; }
        public static List<AccountDb> Accounts { get; set; }

        public static void AddAccount(AccountDb account)
        {
            lock (_locker)
            {
                if (Accounts == null)
                {
                    var providersjson = SecureStorage.GetAsync(StorageKeys.AllAccounts).Result ?? "";
                    Accounts = JsonConvert.DeserializeObject<List<AccountDb>>(providersjson) ?? new List<AccountDb>();
                }

                Accounts.Add(account);
                account.ProviderGuid = account.Provider.Guid;
                SecureStorage.SetAsync(StorageKeys.AllAccounts, JsonConvert.SerializeObject(Accounts)).GetAwaiter().GetResult();
            }
        }

        public static AccountDb GetAccount(Guid accountGuid)
        {
            lock (_locker)
            {
                if (Accounts == null)
                {
                    var providersjson = SecureStorage.GetAsync(StorageKeys.AllAccounts).Result ?? "";
                    Accounts = JsonConvert.DeserializeObject<List<AccountDb>>(providersjson) ?? new List<AccountDb>();
                }
                var accountDb = Accounts.FirstOrDefault(x => x.Guid == accountGuid);
                return accountDb;
            }
        }

        public static void UpdateAccount(AccountDb account)
        {
            lock (_locker)
            {
                if (Accounts == null)
                {
                    var providersjson = SecureStorage.GetAsync(StorageKeys.AllAccounts).Result ?? "";
                    Accounts = JsonConvert.DeserializeObject<List<AccountDb>>(providersjson) ?? new List<AccountDb>();
                }

                if (account.Guid == Guid.Empty)
                    throw new ArgumentNullException("Guid");
                var existingAccount = Accounts.FirstOrDefault(x => x.Guid == account.Guid);
                if (existingAccount != null)
                {
                    CopyAll(account, existingAccount);
                    existingAccount.ProviderGuid = account.Provider?.Guid ?? account.ProviderGuid;
                }
                SecureStorage.SetAsync(StorageKeys.AllAccounts, JsonConvert.SerializeObject(Accounts)).GetAwaiter().GetResult();

            }
        }

        public static void LoadAccountIntoList(ObservableCollection<AccountView> accounts)
        {
            lock (_locker)
            {
                if (Accounts == null)
                {
                    var providersjson = SecureStorage.GetAsync(StorageKeys.AllAccounts).Result ?? "";
                    Accounts = JsonConvert.DeserializeObject<List<AccountDb>>(providersjson) ?? new List<AccountDb>();
                }
                foreach (var accountDb in Accounts)
                {
                    var accountView = new AccountView();
                    CopyAll(accountDb, accountView);
                    accounts.Add(accountView);
                }
            }
        }

        public static void DeleteAccount(AccountView account, Action callback)
        {
            lock (_locker)
            {
                if (Accounts == null)
                {
                    var providersjson = SecureStorage.GetAsync(StorageKeys.AllAccounts).Result ?? "";
                    Accounts = JsonConvert.DeserializeObject<List<AccountDb>>(providersjson) ?? new List<AccountDb>();
                }

                var oldAccount = Accounts.FirstOrDefault(x => x.Guid == account.Guid);
                Accounts.Remove(oldAccount);
                var serializeObject = JsonConvert.SerializeObject(Accounts);
                SecureStorage.SetAsync(StorageKeys.AllAccounts, serializeObject).ContinueWith((result) =>
                {
                    if (callback != null)
                        callback.Invoke();
                });
            }
        }

        public static async Task AddfingerPrintKey(Guid guid, string pin)
        {
            var account = GetAccount(guid);
            var cert = CertHelper.GetCertificateWithKey(guid, pin);
            SecureStorage.SetAsync(account.Thumbprint, Guid.NewGuid().ToString()).GetAwaiter().GetResult();
            var password = SecureStorage.GetAsync(account.Thumbprint).Result;
            SaveCertificateWithFingerPrint(guid, cert.Export(X509ContentType.Pkcs12, password));
            account.HaveFingerprint = true;
            UpdateAccount(account);
        }

        private static void SaveCertificateWithFingerPrint(Guid guid, byte[] export)
        {
            var base64String = Convert.ToBase64String(export);
            SecureStorage.SetAsync("fingerprint_" + guid, base64String).GetAwaiter().GetResult();
        }

        public static X509Certificate2 GetCertificateWithFingerPrint(Guid guid)
        {
            var account = GetAccount(guid);

            var certificateWithFingerPrintBase64 = SecureStorage.GetAsync("fingerprint_" + guid).Result;
            if (certificateWithFingerPrintBase64 != null)
            {
                var bytes = Convert.FromBase64String(certificateWithFingerPrintBase64);
                var password = SecureStorage.GetAsync(account.Thumbprint).Result;
                var cert = new X509Certificate2(bytes, password, X509KeyStorageFlags.Exportable);
                return cert;
            }

            return null;
        }

        private static bool locked = false;
        private static object locker = new object();
        private static DateTime lockerTime;

        public static bool CheckSessionKey(Guid msgSessionId)
        {

            lock (locker)
            {
                if (locked)
                    if (lockerTime.AddMinutes(1) < DateTime.Now)
                        locked = false;
                    else
                        return false;

                locked = true;
                lockerTime = DateTime.Now;
                var result = SecureStorage.GetAsync(msgSessionId.ToString()).Result;
                if (result != null)
                {
                    locked = false;
                    return false;
                }
                return true;
            }
        }
        public static void ReleaseSessionKey(Guid msgSessionId)
        {
            lock (locker)
            {
                SecureStorage.SetAsync(msgSessionId.ToString(), "").GetAwaiter().GetResult();
                locked = false;
            }
        }

        public static string GetDeviceId()
        {
            var deviceId = SecureStorage.GetAsync("deviceId").Result;
            if (deviceId == null)
            {
                deviceId = Guid.NewGuid().ToString();
                SecureStorage.SetAsync("deviceId", deviceId).GetAwaiter().GetResult();
            }
            return deviceId;
        }

        public static List<ProviderDb> LoadProviders()
        {
            if (Providers == null)
            {
                var providersjson = SecureStorage.GetAsync(StorageKeys.ProvidersKey).Result ?? "";
                Providers = JsonConvert.DeserializeObject<List<ProviderDb>>(providersjson) ?? new List<ProviderDb>();
            }

            if (Providers.All(x => x.WebApiUrl != ConfigSettings.WebApiUrl))
                Providers.Add(new ProviderDb()
                {
                    Authorize = ConfigSettings.Authorize,
                    CancelCheck = ConfigSettings.CancelCheck,
                    CheckForStartedSessions = ConfigSettings.CheckForStartedSessions,
                    DeleteAccount = ConfigSettings.DeleteAccount,
                    Name = "passi",
                    WebApiUrl = ConfigSettings.WebApiUrl,
                    SignupCheck = ConfigSettings.SignupCheck,
                    SignupConfirmation = ConfigSettings.SignupConfirmation,
                    SignupPath = ConfigSettings.SignupPath,
                    SyncAccounts = ConfigSettings.SyncAccounts,
                    Time = ConfigSettings.Time,
                    TokenUpdate = ConfigSettings.TokenUpdate,
                    UpdateCertificate = ConfigSettings.UpdateCertificate,
                    IsDefault = true


                });
            if (Debugger.IsAttached && Providers.All(x => x.WebApiUrl != ConfigSettings.WebApiUrlLocal))
                Providers.Add(new ProviderDb()
                {
                    Authorize = ConfigSettings.Authorize,
                    CancelCheck = ConfigSettings.CancelCheck,
                    CheckForStartedSessions = ConfigSettings.CheckForStartedSessions,
                    DeleteAccount = ConfigSettings.DeleteAccount,
                    Name = "passi local",
                    WebApiUrl = ConfigSettings.WebApiUrlLocal,
                    SignupCheck = ConfigSettings.SignupCheck,
                    SignupConfirmation = ConfigSettings.SignupConfirmation,
                    SignupPath = ConfigSettings.SignupPath,
                    SyncAccounts = ConfigSettings.SyncAccounts,
                    Time = ConfigSettings.Time,
                    TokenUpdate = ConfigSettings.TokenUpdate,
                    UpdateCertificate = ConfigSettings.UpdateCertificate,
                });
            SecureStorage.SetAsync(StorageKeys.ProvidersKey, JsonConvert.SerializeObject(Providers)).GetAwaiter().GetResult();

            return Providers;
        }


        public static void DeleteProvider(ProviderDb provider)
        {
            if (Providers == null)
            {
                var providersjson = SecureStorage.GetAsync(StorageKeys.ProvidersKey).Result ?? "";
                Providers = JsonConvert.DeserializeObject<List<ProviderDb>>(providersjson) ?? new List<ProviderDb>();
            }

            Providers.Remove(Providers.First(x => x.Guid == provider.Guid));
            SecureStorage.SetAsync(StorageKeys.ProvidersKey, JsonConvert.SerializeObject(Providers)).GetAwaiter().GetResult();

        }

        public static void UpdateProvider(ProviderDb provider)
        {
            if (Providers == null)
            {
                var providersjson = SecureStorage.GetAsync(StorageKeys.ProvidersKey).Result ?? "";
                Providers = JsonConvert.DeserializeObject<List<ProviderDb>>(providersjson) ?? new List<ProviderDb>();
            }

            var existingProvider = Providers.First(x => x.Guid == provider.Guid);
            CopyAll(provider, existingProvider);

            SecureStorage.SetAsync(StorageKeys.ProvidersKey, JsonConvert.SerializeObject(Providers)).GetAwaiter().GetResult();

        }

        public static void CopyAll<T, R>(T source, R destination)
        {
            var sourceType = typeof(T);
            var destType = typeof(R);
            foreach (var destProperty in destType.GetProperties())
            {
                if (!destProperty.CanWrite)
                    continue;
                var sourceProperty = sourceType.GetProperty(destProperty.Name);
                if (sourceProperty != null)
                {
                    var value = sourceProperty.GetValue(source, null);
                    if (value != null)
                        destProperty.SetValue(destination, destProperty.GetValue(source, null), null);
                }
            }
            foreach (var sourceField in sourceType.GetFields())
            {
                var targetField = sourceType.GetField(sourceField.Name);
                targetField.SetValue(destination, sourceField.GetValue(source));
            }
        }

        public static void AddProvider(ProviderDb provider)
        {
            if (Providers == null)
            {
                var providersjson = SecureStorage.GetAsync(StorageKeys.ProvidersKey).Result ?? "";
                Providers = JsonConvert.DeserializeObject<List<ProviderDb>>(providersjson) ?? new List<ProviderDb>();
            }

            Providers.Add(provider);

            SecureStorage.SetAsync(StorageKeys.ProvidersKey, JsonConvert.SerializeObject(Providers)).GetAwaiter().GetResult();

        }

        public static ProviderDb GetProvider(Guid? guid)
        {
            if (Providers == null)
            {
                var providersjson = SecureStorage.GetAsync(StorageKeys.ProvidersKey).Result ?? "";
                Providers = JsonConvert.DeserializeObject<List<ProviderDb>>(providersjson) ?? new List<ProviderDb>();
            }

            return Providers.First(x => x.Guid == guid);

        }
    }
}