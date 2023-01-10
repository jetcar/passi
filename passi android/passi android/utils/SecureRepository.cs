using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AppCommon;
using AppConfig;
using Newtonsoft.Json;
using passi_android.Menu;
using Xamarin.Essentials;
using CertHelper = passi_android.utils.Certificate.CertHelper;

namespace passi_android.utils
{
    public static class SecureRepository
    {
        private const string ProvidersKey = "providers";
        private static object _locker = new object();

        public static void AddAccount(AccountDb account)
        {
            lock (_locker)
            {
                SecureStorage.GetAsync(StorageKeys.AllAccounts).ContinueWith(
                    (result) =>
                    {
                        var resultResult = result.Result;
                        var allAccounts = resultResult == null ? new List<Account>() : JsonConvert.DeserializeObject<List<Account>>(resultResult);
                        allAccounts.Add(account);
                        account.ProviderName = account.Provider.Name;
                        SecureStorage.SetAsync(StorageKeys.AllAccounts, JsonConvert.SerializeObject(allAccounts)).GetAwaiter().GetResult();
                        SecureStorage.SetAsync(account.Guid.ToString(), JsonConvert.SerializeObject(account)).GetAwaiter().GetResult();
                    });
            }
        }

        public static AccountDb GetAccount(Guid accountGuid)
        {
            lock (_locker)
            {
                var result = SecureStorage.GetAsync(accountGuid.ToString()).Result;
                var accountDb = JsonConvert.DeserializeObject<AccountDb>(result);
                return accountDb;
            }
        }

        public static void UpdateAccount(AccountDb account)
        {
            lock (_locker)
            {
                SecureStorage.GetAsync(StorageKeys.AllAccounts).ContinueWith(
                    (result) =>
                    {
                        if (account.Guid == Guid.Empty)
                            throw new ArgumentNullException("Guid");
                        var resultResult = result.Result;
                        var allAccounts = resultResult == null ? new List<Account>() : JsonConvert.DeserializeObject<List<Account>>(resultResult);
                        var existingAccount = allAccounts.FirstOrDefault(x => x.Guid == account.Guid);
                        if (existingAccount != null)
                        {
                            existingAccount.DeviceId = account.DeviceId;
                            existingAccount.IsConfirmed = account.IsConfirmed;
                            existingAccount.Thumbprint = account.Thumbprint;
                            existingAccount.ValidFrom = account.ValidFrom;
                            existingAccount.ValidTo = account.ValidTo;
                            existingAccount.Inactive = account.Inactive;
                            existingAccount.ProviderName = account.Provider?.Name ?? account.ProviderName;
                        }
                        SecureStorage.SetAsync(StorageKeys.AllAccounts, JsonConvert.SerializeObject(allAccounts)).GetAwaiter().GetResult();
                        SecureStorage.SetAsync(account.Guid.ToString(), JsonConvert.SerializeObject(account)).GetAwaiter().GetResult();
                    });
            }
        }

        public static void LoadAccountIntoList(ObservableCollection<Account> accounts)
        {
            lock (_locker)
            {
                var result = SecureStorage.GetAsync(StorageKeys.AllAccounts).Result;
                if (string.IsNullOrEmpty(result))
                    return;
                var resultResult = result;
                var allAccounts = resultResult == null ? new List<AccountDb>() : JsonConvert.DeserializeObject<List<AccountDb>>(resultResult);
                foreach (var allAccount in allAccounts)
                {
                    accounts.Add(new Account()
                    {
                        Email = allAccount.Email,
                        Guid = allAccount.Guid,
                        ValidTo = allAccount.ValidTo,
                        ValidFrom = allAccount.ValidFrom,
                        IsConfirmed = allAccount.IsConfirmed,
                        DeviceId = allAccount.DeviceId,
                        Thumbprint = allAccount.Thumbprint,
                        Inactive = allAccount.Inactive,
                        ProviderName = allAccount.ProviderName
                    });
                }
            }
        }

        public static void DeleteAccount(Account account, Action callback)
        {
            lock (_locker)
            {
                SecureStorage.GetAsync(StorageKeys.AllAccounts).ContinueWith((result) =>
                {
                    if (string.IsNullOrEmpty(result.Result))
                        return;
                    var resultResult = result.Result;
                    var allAccounts = resultResult == null ? new List<AccountDb>() : JsonConvert.DeserializeObject<List<AccountDb>>(resultResult);
                    var oldAccount = allAccounts.FirstOrDefault(x => x.Guid == account.Guid);
                    allAccounts.Remove(oldAccount);
                    var serializeObject = JsonConvert.SerializeObject(allAccounts);
                    SecureStorage.SetAsync(StorageKeys.AllAccounts, serializeObject).GetAwaiter().GetResult();
                    SecureStorage.Remove(account.Guid.ToString());
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

        private static object locker = new object();

        public static bool AddSessionKey(Guid msgSessionId)
        {
            lock (locker)
            {
                var result = SecureStorage.GetAsync(msgSessionId.ToString()).Result;
                if (result != null)
                    return false;
                SecureStorage.SetAsync(msgSessionId.ToString(), "").GetAwaiter().GetResult();
                return true;
            }
        }

        public static string GetDeviceId()
        {
            var deviceId = SecureStorage.GetAsync("deviceId").Result;
            if (deviceId == null)
            {
                deviceId = Guid.NewGuid().ToString();
                SecureStorage.SetAsync("deviceId", deviceId).RunSynchronously();
            }
            return deviceId;
        }

        public static List<Provider> LoadProvidersIntoList(ObservableCollection<Account> accounts)
        {
            var providersjson = SecureStorage.GetAsync(ProvidersKey).Result ?? "";
            var providers = JsonConvert.DeserializeObject<List<Provider>>(providersjson) ?? new List<Provider>();
            if (providers.All(x => x.WebApiUrl != ConfigSettings.WebApiUrl))
                providers.Add(new Provider()
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
            if (Debugger.IsAttached && providers.All(x => x.WebApiUrl != ConfigSettings.WebApiUrlLocal))
                providers.Add(new Provider()
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
            SecureStorage.SetAsync(ProvidersKey, JsonConvert.SerializeObject(providers)).GetAwaiter().GetResult();
            foreach (var account in accounts)
            {
                var provider = providers.FirstOrDefault(x => x.Name == account.ProviderName);
                if (provider == null)
                {
                    provider = providers.FirstOrDefault(x => x.IsDefault);
                    var tempAccount = GetAccount(account.Guid);
                    tempAccount.Provider = provider;
                    tempAccount.ProviderName = provider.Name;
                    UpdateAccount(tempAccount);
                }
                account.Provider = provider;
                account.ProviderName = provider.Name;
            }
            return providers;
        }

        public static void DeleteProvider(Provider provider)
        {
            var providersjson = SecureStorage.GetAsync(ProvidersKey).Result ?? "";
            var providers = JsonConvert.DeserializeObject<List<Provider>>(providersjson) ?? new List<Provider>();
            providers.Remove(providers.First(x => x.Guid == provider.Guid));
            SecureStorage.SetAsync(ProvidersKey, JsonConvert.SerializeObject(providers)).GetAwaiter().GetResult();

        }
    }
}