using passi_maui.utils;
using RestSharp;
using System.Collections.ObjectModel;
using passi_maui.Registration;

namespace passi_maui
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            this.Version = App.Version;
        }
        private ObservableCollection<utils.AccountView> _accounts = new ObservableCollection<utils.AccountView>();
        private bool _isDeleteVisible;
        private string version = "1";
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

        private void Menu_button(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new Menu.Menu());
        }

        private void Button_PreDeleteAccount(object sender, EventArgs e)
        {
            var account = (utils.AccountView)((ImageButton)sender).BindingContext;
            account.IsDeleteVisible = !account.IsDeleteVisible;
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

        private void Button_AddAccount(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new TermsAgreements());
        }

        private void Button_ShowDeleteAccount(object sender, EventArgs e)
        {
            IsDeleteVisible = !IsDeleteVisible;
            foreach (var account in Accounts)
            {
                account.IsDeleteVisible = false;
            }
        }

        private void Button_Sync(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;
            App.PollNotifications.Invoke();
            button.IsEnabled = true;
        }
    }
}