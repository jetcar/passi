using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AppCommon;
using AppConfig;
using passi_android.Menu;
using passi_android.Registration;
using passi_android.utils;
using RestSharp;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.Xaml;

namespace passi_android
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        private ObservableCollection<utils.AccountView> _accounts = new ObservableCollection<utils.AccountView>();
        private bool _isDeleteVisible;
        private string version = "1";

        public ObservableCollection<utils.AccountView> Accounts
        {
            get { return _accounts ?? (_accounts = new ObservableCollection<utils.AccountView>()); }
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

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            this.Version = App.Version;
        }

        protected override void OnAppearing()
        {
            App.AccountSyncCallback = AccountSyncCallback;
            IsDeleteVisible = false;
            Accounts = new ObservableCollection<utils.AccountView>();
            Task.Run(() =>
            {
                SecureRepository.LoadAccountIntoList(Accounts);
            });
            base.OnAppearing();
            App.PollNotifications.Invoke();
        }


        private void AccountSyncCallback()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Accounts = new ObservableCollection<utils.AccountView>();
                Task.Run(() =>
                {
                    SecureRepository.LoadAccountIntoList(Accounts);
                });
            });
        }

        private void Button_AddAccount(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new TermsAgreements());
        }

        private void Button_DeleteAccount(object sender, EventArgs e)
        {
            var account = (utils.AccountView)((Button)sender).BindingContext;
            SecureRepository.DeleteAccount(account, () =>
            {
                Accounts.Clear();
                SecureRepository.LoadAccountIntoList(Accounts);
                var provider = SecureRepository.GetProvider(account.ProviderGuid);
                RestService.ExecuteAsync(provider, provider.DeleteAccount + $"?accountGuid{account.Guid}&thumbprint={account.Thumbprint}", Method.Delete);
            });
        }

        private void Button_PreDeleteAccount(object sender, EventArgs e)
        {
            var account = (utils.AccountView)((ImageButton)sender).BindingContext;
            account.IsDeleteVisible = !account.IsDeleteVisible;
        }

        private void Cell_OnTapped(object sender, EventArgs e)
        {
            var cell = sender as ViewCell;
            cell.IsEnabled = false;

            var account = (utils.AccountView)((ViewCell)sender).BindingContext;
            var accountDb = SecureRepository.GetAccount(account.Guid);
            var provider = SecureRepository.GetProvider(accountDb.ProviderGuid);
            accountDb.Provider = provider;
            if (!accountDb.IsConfirmed || accountDb.PublicCertBinary == null)
            {
                Navigation.PushModalSinglePage(new RegistrationConfirmation(accountDb));
            }
            else
            {
                Navigation.PushModalSinglePage(new AccountView(accountDb));
            }
            cell.IsEnabled = true;
        }

        private void Button_Sync(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;
            App.PollNotifications.Invoke();
            button.IsEnabled = true;
        }

        private void Button_ShowDeleteAccount(object sender, EventArgs e)
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

        private void Menu_button(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new Menu.Menu());

        }
    }
}