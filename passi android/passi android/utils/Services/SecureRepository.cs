using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AppConfig;
using Newtonsoft.Json;
using passi_android.StorageModels;
using passi_android.utils.Services.Certificate;
using passi_android.ViewModels;
using Xamarin.Essentials;
using CertHelper = passi_android.utils.Services.Certificate.CertHelper;

namespace passi_android.utils.Services
{
    public class SecureRepository : ISecureRepository
    {
        private static object _locker = new object();
        List<ProviderDb> Providers { get; set; }
        List<AccountDb> Accounts { get; set; }

        private ICertConverter _certConverter;
        private IMySecureStorage _mySecureStorage;
        public void AddAccount(AccountDb account)
        {
            lock (_locker)
            {
                if (Accounts == null)
                {
                    Accounts = _mySecureStorage.GetAsync<List<AccountDb>>(StorageKeys.AllAccounts).Result;
                }

                Accounts.Add(account);
                account.ProviderGuid = account.Provider.Guid;
                _mySecureStorage.SetAsync(StorageKeys.AllAccounts, Accounts).GetAwaiter().GetResult();
            }
        }

        public AccountDb GetAccount(Guid accountGuid)
        {
            lock (_locker)
            {
                if (Accounts == null)
                {
                    Accounts = _mySecureStorage.GetAsync<List<AccountDb>>(StorageKeys.AllAccounts).Result;
                }
                var accountDb = Accounts.FirstOrDefault(x => x.Guid == accountGuid);
                return accountDb;
            }
        }

        public void UpdateAccount(AccountDb account)
        {
            lock (_locker)
            {
                if (Accounts == null)
                {
                    Accounts = _mySecureStorage.GetAsync<List<AccountDb>>(StorageKeys.AllAccounts).Result;
                }

                if (account.Guid == Guid.Empty)
                    throw new ArgumentNullException("Guid");
                var existingAccount = Accounts.FirstOrDefault(x => x.Guid == account.Guid);
                if (existingAccount != null)
                {
                    CopyAll(account, existingAccount);
                    existingAccount.ProviderGuid = account.Provider?.Guid ?? account.ProviderGuid;
                }
                _mySecureStorage.SetAsync(StorageKeys.AllAccounts, Accounts).GetAwaiter().GetResult();

            }
        }

        public void LoadAccountIntoList(ObservableCollection<AccountViewModel> accounts)
        {
            lock (_locker)
            {
                if (Accounts == null)
                {
                    Accounts = _mySecureStorage.GetAsync<List<AccountDb>>(StorageKeys.AllAccounts).Result;
                }
                foreach (var accountDb in Accounts)
                {
                    var accountView = new AccountViewModel();
                    CopyAll(accountDb, accountView);
                    accounts.Add(accountView);
                }
            }
        }

        public void DeleteAccount(AccountViewModel account, Action callback)
        {
            lock (_locker)
            {
                if (Accounts == null)
                {
                    Accounts = _mySecureStorage.GetAsync<List<AccountDb>>(StorageKeys.AllAccounts).Result;
                }

                var oldAccount = Accounts.FirstOrDefault(x => x.Guid == account.Guid);
                Accounts.Remove(oldAccount);
                _mySecureStorage.SetAsync(StorageKeys.AllAccounts, Accounts).ContinueWith((result) =>
                {
                    if (callback != null)
                        callback.Invoke();
                });
            }
        }

        public async Task AddfingerPrintKey(AccountDb account, MySecureString pin)
        {
            var cert = _certConverter.GetCertificateWithKey(pin, account);
            _mySecureStorage.SetAsync(account.Thumbprint, Guid.NewGuid().ToString()).GetAwaiter().GetResult();
            var password = _mySecureStorage.GetAsync(account.Thumbprint).Result;
            SaveCertificateWithFingerPrint(account.Guid, cert.Export(X509ContentType.Pkcs12, password));
            account.HaveFingerprint = true;
            UpdateAccount(account);

        }

        public void SaveCertificateWithFingerPrint(Guid guid, byte[] export)
        {
            var base64String = Convert.ToBase64String(export);
            _mySecureStorage.SetAsync("fingerprint_" + guid, base64String).GetAwaiter().GetResult();
        }

        public X509Certificate2 GetCertificateWithFingerPrint(Guid guid)
        {
            var account = GetAccount(guid);

            var certificateWithFingerPrintBase64 = _mySecureStorage.GetAsync("fingerprint_" + guid).Result;
            if (certificateWithFingerPrintBase64 != null)
            {
                var bytes = Convert.FromBase64String(certificateWithFingerPrintBase64);
                var password = _mySecureStorage.GetAsync(account.Thumbprint).Result;
                var cert = new X509Certificate2(bytes, password, X509KeyStorageFlags.Exportable);
                return cert;
            }

            return null;
        }

        private bool locked = false;
        private object locker = new object();
        private DateTime lockerTime;

        public SecureRepository(ICertConverter certConverter, IMySecureStorage mySecureStorage)
        {
            _certConverter = certConverter;
            _mySecureStorage = mySecureStorage;
        }


        public bool CheckSessionKey(Guid msgSessionId)
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
                var result = _mySecureStorage.GetAsync(msgSessionId.ToString()).Result;
                if (result != null)
                {
                    locked = false;
                    return false;
                }
                return true;
            }
        }
        public void ReleaseSessionKey(Guid msgSessionId)
        {
            lock (locker)
            {
                _mySecureStorage.SetAsync(msgSessionId.ToString(), "").GetAwaiter().GetResult();
                locked = false;
            }
        }

        public string GetDeviceId()
        {
            var deviceId = _mySecureStorage.GetAsync("deviceId").Result;
            if (deviceId == null)
            {
                deviceId = Guid.NewGuid().ToString();
                _mySecureStorage.SetAsync("deviceId", deviceId).GetAwaiter().GetResult();
            }
            return deviceId;
        }

        public List<ProviderDb> LoadProviders()
        {
            lock (locker)
            {
                if (Providers == null)
                {
                    Providers = _mySecureStorage.GetAsync<List<ProviderDb>>(StorageKeys.ProvidersKey).Result;
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
                        SignupPath = ConfigSettings.Signup,
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
                        SignupPath = ConfigSettings.Signup,
                        SyncAccounts = ConfigSettings.SyncAccounts,
                        Time = ConfigSettings.Time,
                        TokenUpdate = ConfigSettings.TokenUpdate,
                        UpdateCertificate = ConfigSettings.UpdateCertificate,
                    });
                _mySecureStorage.SetAsync(StorageKeys.ProvidersKey, Providers).GetAwaiter().GetResult();

                return Providers;
            }
        }


        public void DeleteProvider(ProviderDb provider)
        {
            if (Providers == null)
            {
                Providers = _mySecureStorage.GetAsync<List<ProviderDb>>(StorageKeys.ProvidersKey).Result;
            }

            Providers.Remove(Providers.First(x => x.Guid == provider.Guid));
            _mySecureStorage.SetAsync(StorageKeys.ProvidersKey, Providers).GetAwaiter().GetResult();

        }

        public void UpdateProvider(ProviderDb provider)
        {
            if (Providers == null)
            {
                Providers = _mySecureStorage.GetAsync<List<ProviderDb>>(StorageKeys.ProvidersKey).Result;
            }

            var existingProvider = Providers.First(x => x.Guid == provider.Guid);
            CopyAll(provider, existingProvider);

            _mySecureStorage.SetAsync(StorageKeys.ProvidersKey, Providers).GetAwaiter().GetResult();

        }

        public void CopyAll<T, R>(T source, R destination)
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

        public void AddProvider(ProviderDb provider)
        {
            if (Providers == null)
            {
                Providers = _mySecureStorage.GetAsync<List<ProviderDb>>(StorageKeys.ProvidersKey).Result;
            }

            Providers.Add(provider);

            _mySecureStorage.SetAsync(StorageKeys.ProvidersKey, Providers).GetAwaiter().GetResult();

        }

        public ProviderDb GetProvider(Guid? guid)
        {
            if (Providers == null)
            {
                Providers = _mySecureStorage.GetAsync<List<ProviderDb>>(StorageKeys.ProvidersKey).Result;
            }

            return Providers.First(x => x.Guid == guid);

        }
    }

    public interface ISecureRepository
    {
        void AddAccount(AccountDb account);
        AccountDb GetAccount(Guid accountGuid);
        void UpdateAccount(AccountDb account);
        void LoadAccountIntoList(ObservableCollection<AccountViewModel> accounts);
        void DeleteAccount(AccountViewModel account, Action callback);
        Task AddfingerPrintKey(AccountDb account, MySecureString pin);
        void SaveCertificateWithFingerPrint(Guid guid, byte[] export);
        X509Certificate2 GetCertificateWithFingerPrint(Guid guid);
        bool CheckSessionKey(Guid msgSessionId);
        void ReleaseSessionKey(Guid msgSessionId);
        string GetDeviceId();
        List<ProviderDb> LoadProviders();
        void DeleteProvider(ProviderDb provider);
        void UpdateProvider(ProviderDb provider);
        void CopyAll<T, R>(T source, R destination);
        void AddProvider(ProviderDb provider);
        ProviderDb GetProvider(Guid? guid);
    }


}