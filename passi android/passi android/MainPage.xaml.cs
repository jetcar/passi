using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using passi_android.Registration;
using passi_android.utils;
using RestSharp;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        private ObservableCollection<utils.AccountView> _accounts = new ObservableCollection<utils.AccountView>();
        private bool _isDeleteVisible;
        private string version = "1";
        private ISecureRepository _secureRepository;
        IRestService _restService;
        ISyncService _syncService;
        INavigationService Navigation;
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
            _secureRepository = App.Services.GetService<ISecureRepository>();
            _restService = App.Services.GetService<IRestService>();
            _syncService = App.Services.GetService<ISyncService>();
            Navigation = App.Services.GetService<INavigationService>();

            if(!App.IsTest)
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
                _secureRepository.LoadAccountIntoList(Accounts);
            });
            base.OnAppearing();

            _syncService.PollNotifications();
        }


        private void AccountSyncCallback()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Accounts = new ObservableCollection<utils.AccountView>();
                Task.Run(() =>
                {
                    _secureRepository.LoadAccountIntoList(Accounts);
                });
            });
        }

        public void Button_AddAccount(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new TermsAgreements());
        }

        public void Button_DeleteAccount(object sender, EventArgs e)
        {
            var account = (utils.AccountView)((Button)sender).BindingContext;
            _secureRepository.DeleteAccount(account, () =>
            {
                Accounts.Clear();
                _secureRepository.LoadAccountIntoList(Accounts);
                var provider = _secureRepository.GetProvider(account.ProviderGuid);
                _restService.ExecuteAsync(provider, provider.DeleteAccount + $"?accountGuid{account.Guid}&thumbprint={account.Thumbprint}", Method.Delete);
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
            var accountDb = _secureRepository.GetAccount(account.Guid);
            var provider = _secureRepository.GetProvider(accountDb.ProviderGuid);
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

        public void Button_Sync(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;
            _syncService.PollNotifications();
            button.IsEnabled = true;
        }

        public void Button_ShowDeleteAccount(object sender, EventArgs e)
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