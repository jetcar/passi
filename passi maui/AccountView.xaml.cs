using passi_maui.utils;

namespace passi_maui
{
    [QueryProperty("Account", "Account")]
    public partial class AccountView : ContentPage
    {
        public AccountDb Account
        {
            get => _account;
            set
            {
                if (Equals(value, _account)) return;
                _account = value;
                OnPropertyChanged();
            }
        }

        private string _email;
        private string _thumbprint;
        private string _validFrom;
        private string _validTo;
        private string _providerName;
        private AccountDb _account;

        public AccountView()
        {
            InitializeComponent();
            BindingContext = this;

           
        }

        public string ProviderName
        {
            get => _providerName;
            set
            {
                if (value == _providerName) return;
                _providerName = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        public string Thumbprint
        {
            get => _thumbprint;
            set
            {
                _thumbprint = value;
                OnPropertyChanged();
            }
        }

        public string ValidFrom
        {
            get => _validFrom;
            set
            {
                _validFrom = value;
                OnPropertyChanged();
            }
        }

        public string ValidTo
        {
            get => _validTo;
            set
            {
                _validTo = value;
                OnPropertyChanged();
            }
        }

        protected override void OnAppearing()
        {
            try
            {
                Email = Account.Email;
                Thumbprint = Account.Thumbprint;
                ValidFrom = Account.ValidFrom.ToShortDateString();
                ValidTo = Account.ValidTo.ToShortDateString();
                Email = Account.Email;
                Thumbprint = Account.Thumbprint;
                ProviderName = Account.Provider?.Name;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            base.OnAppearing();
        }

        private void UpdateCertificate_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;
            if (Account.pinLength > 0)
                Navigation.PushModalSinglePage(new UpdateCertificate(),new Dictionary<string, object>() { {"Account", Account} });
            else
            {
                UpdateCertificate.StartCertGeneration(null, null, Account, Navigation, (error) =>
                {

                });
            }
            button.IsEnabled = true;
        }

        private void AddBiometric_Button_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            Navigation.PushModalSinglePage(new FingerPrint.FingerPrintView(),new Dictionary<string, object>() { {"Account",Account}});
            button.IsEnabled = true;
        }

        private void Button_Back(object sender, EventArgs e)
        {
            Navigation.PopModal();
        }
    }
}