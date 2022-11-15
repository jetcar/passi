using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AppCommon;
using Newtonsoft.Json;
using Xamarin.Essentials;
using CertHelper = passi_android.utils.Certificate.CertHelper;

namespace passi_android.utils
{
    public static class SecureRepository
    {
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
                        SecureStorage.SetAsync(StorageKeys.AllAccounts, JsonConvert.SerializeObject(allAccounts));
                        SecureStorage.SetAsync(account.Guid.ToString(), JsonConvert.SerializeObject(account));
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
                        }
                        SecureStorage.SetAsync(StorageKeys.AllAccounts, JsonConvert.SerializeObject(allAccounts));
                        SecureStorage.SetAsync(account.Guid.ToString(), JsonConvert.SerializeObject(account));
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
                        Inactive = allAccount.Inactive
                    });
                }
            }
        }

        public static void DeleteAccount(Guid accountGuid, Action callback)
        {
            lock (_locker)
            {
                SecureStorage.GetAsync(StorageKeys.AllAccounts).ContinueWith((result) =>
                {
                    if (string.IsNullOrEmpty(result.Result))
                        return;
                    var resultResult = result.Result;
                    var allAccounts = resultResult == null ? new List<AccountDb>() : JsonConvert.DeserializeObject<List<AccountDb>>(resultResult);
                    var oldAccount = allAccounts.FirstOrDefault(x => x.Guid == accountGuid);
                    allAccounts.Remove(oldAccount);
                    var serializeObject = JsonConvert.SerializeObject(allAccounts);
                    SecureStorage.SetAsync(StorageKeys.AllAccounts, serializeObject);
                    SecureStorage.Remove(accountGuid.ToString());
                    if (callback != null)
                        callback.Invoke();
                });
            }
        }

        public static async Task AddfingerPrintKey(Guid guid, string pin)
        {
            var account = GetAccount(guid);
            var cert = CertHelper.GetCertificateWithKey(guid, pin);
            await SecureStorage.SetAsync(account.Thumbprint, Guid.NewGuid().ToString());
            var password = SecureStorage.GetAsync(account.Thumbprint).Result;
            SaveCertificateWithFingerPrint(guid, cert.Export(X509ContentType.Pkcs12, password));
            account.HaveFingerprint = true;
            UpdateAccount(account);
        }

        private static void SaveCertificateWithFingerPrint(Guid guid, byte[] export)
        {
            var base64String = Convert.ToBase64String(export);
            SecureStorage.SetAsync("fingerprint_" + guid, base64String);
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
                SecureStorage.SetAsync("deviceId", deviceId);
            }
            return deviceId;
        }
    }
}