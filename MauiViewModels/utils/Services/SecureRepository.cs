using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AppConfig;
using MauiCommonServices;
using MauiViewModels.StorageModels;
using MauiViewModels.utils.Services.Certificate;
using MauiViewModels.ViewModels;

namespace MauiViewModels.utils.Services;

public class SecureRepository : ISecureRepository
{
    private static object _locker = new object();
    private List<ProviderDb> Providers { get; set; }
    private List<AccountDb> AccountsDb { get; set; }

    private IMySecureStorage _mySecureStorage;

    public SecureRepository(IMySecureStorage mySecureStorage)
    {
        _mySecureStorage = mySecureStorage;
    }

    public void AddAccount(AccountDb account)
    {
        lock (_locker)
        {
            if (AccountsDb == null)
            {
                AccountsDb = _mySecureStorage.GetAsync<List<AccountDb>>(StorageKeys.AllAccounts).Result ?? new List<AccountDb>();
            }

            AccountsDb.Add(account);
            account.ProviderGuid = account.Provider.Guid;
            _mySecureStorage.SetAsync(StorageKeys.AllAccounts, AccountsDb).GetAwaiter().GetResult();
        }
    }

    public AccountDb GetAccount(Guid accountGuid)
    {
        lock (_locker)
        {
            if (AccountsDb == null)
            {
                AccountsDb = _mySecureStorage.GetAsync<List<AccountDb>>(StorageKeys.AllAccounts).Result ?? new List<AccountDb>();
            }
            var accountDb = AccountsDb.FirstOrDefault(x => x.Guid == accountGuid);
            return accountDb;
        }
    }

    public void UpdateAccount(AccountDb account)
    {
        lock (_locker)
        {
            if (AccountsDb == null)
            {
                AccountsDb = _mySecureStorage.GetAsync<List<AccountDb>>(StorageKeys.AllAccounts).Result ?? new List<AccountDb>();
            }

            if (account.Guid == Guid.Empty)
                throw new ArgumentNullException("Guid");
            var existingAccount = AccountsDb.FirstOrDefault(x => x.Guid == account.Guid);
            if (existingAccount != null)
            {
                CopyAll(account, existingAccount);
                existingAccount.ProviderGuid = account.Provider?.Guid ?? account.ProviderGuid;
            }
            _mySecureStorage.SetAsync(StorageKeys.AllAccounts, AccountsDb).GetAwaiter().GetResult();
        }
    }

    public void LoadAccountIntoList(ObservableCollection<AccountModel> accountViews)
    {
        lock (_locker)
        {
            if (AccountsDb == null)
            {
                AccountsDb = _mySecureStorage.GetAsync<List<AccountDb>>(StorageKeys.AllAccounts).Result ?? new List<AccountDb>();
            }

            for (int i = 0; i < AccountsDb.Count; i++)
            {
                if (accountViews.Count < i + 1)
                    accountViews.Add(new AccountModel());
                CopyAll(AccountsDb[i], accountViews[i]);
            }

            while (AccountsDb.Count > accountViews.Count)
            {
                accountViews.RemoveAt(accountViews.Count - 1);
            }
        }
    }

    public void DeleteAccount(AccountModel account, Action callback)
    {
        lock (_locker)
        {
            if (AccountsDb == null)
            {
                AccountsDb = _mySecureStorage.GetAsync<List<AccountDb>>(StorageKeys.AllAccounts).Result ?? new List<AccountDb>();
            }

            var oldAccount = AccountsDb.FirstOrDefault(x => x.Guid == account.Guid);
            AccountsDb.Remove(oldAccount);
            _mySecureStorage.SetAsync(StorageKeys.AllAccounts, AccountsDb).ContinueWith((result) =>
            {
                if (callback != null)
                    callback.Invoke();
            });
        }
    }

    public async Task SaveFingerPrintKey(AccountDb account, X509Certificate2 cert)
    {
        var password = Guid.NewGuid().ToString();
        _mySecureStorage.SetAsync(account.Thumbprint, password).GetAwaiter().GetResult();
        //var password = _mySecureStorage.GetAsync(account.Thumbprint).Result;
        var base64String = Convert.ToBase64String(cert.Export(X509ContentType.Pkcs12, password));
        _mySecureStorage.SetAsync("fingerprint_" + account.Guid, base64String).GetAwaiter().GetResult();
        account.HaveFingerprint = true;
        UpdateAccount(account);
    }

    public X509Certificate2 GetCertificateWithFingerPrint(AccountDb account)
    {
        var certificateWithFingerPrintBase64 = _mySecureStorage.GetAsync("fingerprint_" + account.Guid).Result;
        if (certificateWithFingerPrintBase64 != null)
        {
            var bytes = Convert.FromBase64String(certificateWithFingerPrintBase64);
            var password = _mySecureStorage.GetAsync(account.Thumbprint).Result;
            var cert = new X509Certificate2(bytes, password, X509KeyStorageFlags.Exportable);
            return cert;
        }
        return null;
    }

    public X509Certificate2 GetCertificateWithKey(MySecureString pin, AccountDb account)
    {
        var mySecureString = new MySecureString(account.Salt);
        if (pin != null)
            mySecureString.Append(pin);
        var secureStringToString = mySecureString.SecureStringToString();
        return new X509Certificate2(Convert.FromBase64String(account.PrivateCertBinary), secureStringToString, X509KeyStorageFlags.Exportable);
    }

    private bool locked = false;
    private object locker = new object();
    private DateTime lockerTime;

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
            if (!string.IsNullOrEmpty(result))
            {
                locked = false;
                return false;
            }
            _mySecureStorage.SetAsync(msgSessionId.ToString(), "used").GetAwaiter().GetResult();
            return true;
        }
    }

    public void ReleaseSessionKey(Guid msgSessionId)
    {
        lock (locker)
        {
            locked = false;
        }
    }

    public string GetDeviceId()
    {
        var deviceId = _mySecureStorage.GetAsync("deviceId").Result;
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            _mySecureStorage.SetAsync("deviceId", deviceId).GetAwaiter().GetResult();
        }
        return deviceId;
    }

    public string GetReplyId()
    {
        var deviceId = _mySecureStorage.GetAsync("replyId").Result;
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            _mySecureStorage.SetAsync("replyId", deviceId).GetAwaiter().GetResult();
        }
        return deviceId;
    }

    public string SetReplyId()
    {
        var deviceId = Guid.NewGuid().ToString();
        _mySecureStorage.SetAsync("replyId", deviceId).GetAwaiter().GetResult();

        return deviceId;
    }

    public Task<List<ProviderDb>> LoadProviders()
    {
        lock (locker)
        {
            if (Providers == null)
            {
                Providers = _mySecureStorage.GetAsync<List<ProviderDb>>(StorageKeys.ProvidersKey).Result ?? new List<ProviderDb>();
                foreach (var provider in Providers.Where(x => x.PassiWebApiUrl == null).ToList())
                {
                    Providers.Remove(provider);
                }
            }

            if (Providers.All(x => x.PassiWebApiUrl != ConfigSettings.PassiUrl))
                Providers.Add(new ProviderDb()
                {
                    Authorize = ConfigSettings.Authorize,
                    CancelCheck = ConfigSettings.CancelCheck,
                    CheckForStartedSessions = ConfigSettings.CheckForStartedSessions,
                    DeleteAccount = ConfigSettings.DeleteAccount,
                    Name = "passi",
                    PassiWebApiUrl = ConfigSettings.PassiUrl,
                    SignupCheck = ConfigSettings.SignupCheck,
                    SignupConfirmation = ConfigSettings.SignupConfirmation,
                    SignupPath = ConfigSettings.Signup,
                    SyncAccounts = ConfigSettings.SyncAccounts,
                    Time = ConfigSettings.Time,
                    TokenUpdate = ConfigSettings.TokenUpdate,
                    UpdateCertificate = ConfigSettings.UpdateCertificate,
                    IsDefault = true
                });
            if (Debugger.IsAttached && Providers.All(x => x.PassiWebApiUrl != ConfigSettings.WebApiUrlLocal))
                Providers.Add(new ProviderDb()
                {
                    Authorize = ConfigSettings.Authorize,
                    CancelCheck = ConfigSettings.CancelCheck,
                    CheckForStartedSessions = ConfigSettings.CheckForStartedSessions,
                    DeleteAccount = ConfigSettings.DeleteAccount,
                    Name = "passi local",
                    PassiWebApiUrl = ConfigSettings.WebApiUrlLocal,
                    SignupCheck = ConfigSettings.SignupCheck,
                    SignupConfirmation = ConfigSettings.SignupConfirmation,
                    SignupPath = ConfigSettings.Signup,
                    SyncAccounts = ConfigSettings.SyncAccounts,
                    Time = ConfigSettings.Time,
                    TokenUpdate = ConfigSettings.TokenUpdate,
                    UpdateCertificate = ConfigSettings.UpdateCertificate,
                });
            _mySecureStorage.SetAsync(StorageKeys.ProvidersKey, Providers).GetAwaiter().GetResult();

            return Task.FromResult(Providers);
        }
    }

    public void DeleteProvider(ProviderDb provider)
    {
        if (Providers == null)
        {
            Providers = _mySecureStorage.GetAsync<List<ProviderDb>>(StorageKeys.ProvidersKey).Result ?? new List<ProviderDb>();
        }

        Providers.Remove(Providers.First(x => x.Guid == provider.Guid));
        _mySecureStorage.SetAsync(StorageKeys.ProvidersKey, Providers).GetAwaiter().GetResult();
    }

    public void UpdateProvider(ProviderDb provider)
    {
        if (Providers == null)
        {
            Providers = _mySecureStorage.GetAsync<List<ProviderDb>>(StorageKeys.ProvidersKey).Result ?? new List<ProviderDb>();
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
            if (!destProperty.CanWrite || destProperty.Name == "IsDeleteVisible")
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
            Providers = _mySecureStorage.GetAsync<List<ProviderDb>>(StorageKeys.ProvidersKey).Result ?? new List<ProviderDb>();
        }

        Providers.Add(provider);

        _mySecureStorage.SetAsync(StorageKeys.ProvidersKey, Providers).GetAwaiter().GetResult();
    }

    public ProviderDb GetProvider(Guid? guid)
    {
        if (Providers == null)
        {
            Providers = _mySecureStorage.GetAsync<List<ProviderDb>>(StorageKeys.ProvidersKey).Result ?? new List<ProviderDb>();
        }

        return Providers.First(x => x.Guid == guid);
    }
}

public interface ISecureRepository
{
    X509Certificate2 GetCertificateWithKey(MySecureString pin, AccountDb account);

    void AddAccount(AccountDb account);

    AccountDb GetAccount(Guid accountGuid);

    void UpdateAccount(AccountDb account);

    void LoadAccountIntoList(ObservableCollection<AccountModel> accountViews);

    void DeleteAccount(AccountModel account, Action callback);

    Task SaveFingerPrintKey(AccountDb account, X509Certificate2 cert);

    X509Certificate2 GetCertificateWithFingerPrint(AccountDb account);

    bool CheckSessionKey(Guid msgSessionId);

    void ReleaseSessionKey(Guid msgSessionId);

    string GetDeviceId();

    Task<List<ProviderDb>> LoadProviders();

    void DeleteProvider(ProviderDb provider);

    void UpdateProvider(ProviderDb provider);

    void CopyAll<T, R>(T source, R destination);

    void AddProvider(ProviderDb provider);

    ProviderDb GetProvider(Guid? guid);

    string GetReplyId();

    string SetReplyId();
}