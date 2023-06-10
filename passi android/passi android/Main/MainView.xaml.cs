using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using passi_android.Registration;
using passi_android.ViewModels;
using RestSharp;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Main
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainView : BaseContentPage
    {
        private ObservableCollection<AccountViewModel> _accounts = new ObservableCollection<AccountViewModel>();
        private bool _isDeleteVisible;
        private string version = "1";
        public Task _loadAccountTask;

        public ObservableCollection<AccountViewModel> Accounts
        {
            get { return _accounts ?? (_accounts = new ObservableCollection<AccountViewModel>()); }
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
            if (!App.IsTest)
                InitializeComponent();
            BindingContext = this;

            this.Version = App.Version;
        }

        protected override void OnAppearing()
        {
            App.AccountSyncCallback = AccountSyncCallback;
            IsDeleteVisible = false;
            Accounts = new ObservableCollection<AccountViewModel>();
            _loadAccountTask = Task.Run(() =>
            {
                _secureRepository.LoadAccountIntoList(Accounts);
            });
            base.OnAppearing();
            _syncService.PollNotifications();


        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }


        private void AccountSyncCallback()
        {
            _mainThreadService.BeginInvokeOnMainThread(() =>
            {
                Accounts = new ObservableCollection<AccountViewModel>();
                _loadAccountTask = Task.Run(() =>
                {
                    _secureRepository.LoadAccountIntoList(Accounts);
                });
            });
        }

        public void Button_AddAccount(object sender, EventArgs e)
        {
            _navigationService.PushModalSinglePage(new TermsAgreementsView());
        }

        public void Button_DeleteAccount(object sender, EventArgs e)
        {
            var account = (AccountViewModel)((Button)sender).BindingContext;
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
            var account = (AccountViewModel)((ImageButton)sender).BindingContext;
            account.IsDeleteVisible = !account.IsDeleteVisible;
        }

        public void Cell_OnTapped(object sender, EventArgs e)
        {
            var cell = sender as ViewCell;
            cell.IsEnabled = false;

            var account = (AccountViewModel)((ViewCell)sender).BindingContext;
            var accountDb = _secureRepository.GetAccount(account.Guid);
            var provider = _secureRepository.GetProvider(accountDb.ProviderGuid);
            accountDb.Provider = provider;
            if (!accountDb.IsConfirmed || accountDb.PublicCertBinary == null)
            {
                _navigationService.PushModalSinglePage(new RegistrationConfirmationView(accountDb));
            }
            else
            {
                _navigationService.PushModalSinglePage(new AccountView(accountDb));
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
            _navigationService.PushModalSinglePage(new Menu.MenuView());

        }
    }
}