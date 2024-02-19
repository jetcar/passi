using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MauiViewModels.Main;
using MauiViewModels.Registration;
using MauiViewModels.ViewModels;
using RestSharp;
using AccountViewModel = MauiViewModels.Main.AccountViewModel;

namespace MauiViewModels;

public class MainView : BaseViewModel
{
    private ObservableCollection<ViewModels.AccountModel> _accounts = new ObservableCollection<ViewModels.AccountModel>();
    private bool _isDeleteVisible;
    private string version = "1";
    public Task _loadAccountTask;

    public ObservableCollection<ViewModels.AccountModel> Accounts
    {
        get { return _accounts ?? (_accounts = new ObservableCollection<ViewModels.AccountModel>()); }
        set
        {
            if (_accounts != value)
            {
                _accounts = value;
                OnPropertyChanged();
            }
        }
    }

    public string Version
    {
        get => version;
        set
        {
            version = value;
            OnPropertyChanged();
        }
    }

    public MainView()
    {
        _secureRepository.LoadProviders();
        this.Version = CommonApp.Version;
    }

    public override void OnAppearing(object sender, EventArgs eventArgs)
    {
        CommonApp.AccountSyncCallback = AccountSyncCallback;
        IsDeleteVisible = false;
        Accounts = new ObservableCollection<ViewModels.AccountModel>();
        _loadAccountTask = Task.Run(() =>
        {
            _secureRepository.LoadAccountIntoList(Accounts);
            //redirect test
            var accountDb = _secureRepository.GetAccount(Accounts[0].Guid);
            _navigationService.PushModalSinglePage(new RegistrationConfirmationViewModel(accountDb));
        });
        base.OnAppearing(sender, eventArgs);
        _syncService.PollNotifications();
    }

    private void AccountSyncCallback()
    {
        _mainThreadService.BeginInvokeOnMainThread(() =>
        {
            Accounts = new ObservableCollection<ViewModels.AccountModel>();
            _loadAccountTask = Task.Run(() =>
            {
                _secureRepository.LoadAccountIntoList(Accounts);
            });
        });
    }

    public void Button_AddAccount()
    {
        _navigationService.PushModalSinglePage(new TermsAgreementsViewModel());
    }

    public void Button_DeleteAccount(ViewModels.AccountModel account)
    {
        _secureRepository.DeleteAccount(account, () =>
        {
            Accounts.Clear();
            _secureRepository.LoadAccountIntoList(Accounts);
            var provider = _secureRepository.GetProvider(account.ProviderGuid);
            _restService.ExecuteAsync(provider, provider.DeleteAccount + $"?accountGuid{account.Guid}&thumbprint={account.Thumbprint}", Method.Delete);
        });
    }

    public void Button_PreDeleteAccount(ViewModels.AccountModel account)
    {
        account.IsDeleteVisible = !account.IsDeleteVisible;
    }

    public void Cell_OnTapped(ViewModels.AccountModel account)
    {
        var accountDb = _secureRepository.GetAccount(account.Guid);
        var provider = _secureRepository.GetProvider(accountDb.ProviderGuid);
        accountDb.Provider = provider;
        if (!accountDb.IsConfirmed || accountDb.PublicCertBinary == null)
        {
            _navigationService.PushModalSinglePage(new RegistrationConfirmationViewModel(accountDb));
        }
        else
        {
            _navigationService.PushModalSinglePage(new AccountViewModel(accountDb));
        }
    }

    public void Button_Sync()
    {
        _syncService.PollNotifications();
    }

    public void Button_ShowDeleteAccount()
    {
        IsDeleteVisible = !IsDeleteVisible;
        foreach (var account in Accounts)
        {
            account.IsDeleteVisible = false;
        }
    }

    public bool IsDeleteVisible
    {
        get
        {
            return _isDeleteVisible;
        }
        set
        {
            _isDeleteVisible = value;
            OnPropertyChanged();
        }
    }

    public void Menu_button()
    {
        _navigationService.PushModalSinglePage(new Menu.MenuViewModel());
    }
}