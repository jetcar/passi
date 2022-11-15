using System;
using AppCommon;
using passi_android.utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AccountView : ContentPage
    {
        public AccountDb AccountDb { get; set; }
        private string _email;
        private string _thumbprint;
        private string _validFrom;
        private string _validTo;

        public AccountView(AccountDb accountDb)
        {
            AccountDb = accountDb;
            InitializeComponent();
            BindingContext = this;

            try
            {
                Email = AccountDb.Email;
                Thumbprint = AccountDb.Thumbprint;
                ValidFrom = AccountDb.ValidFrom.ToShortDateString();
                ValidTo = AccountDb.ValidTo.ToShortDateString();
                Email = accountDb.Email;
                Thumbprint = AccountDb.Thumbprint;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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

        private void Button_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            Navigation.PushModalSinglePage(new UpdateCertificate() { Account = AccountDb });
            button.IsEnabled = true;
        }

        private void AddBiometric_Button_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            Navigation.PushModalSinglePage(new FingerPrint.FingerPrintView(AccountDb));
            button.IsEnabled = true;
        }
    }
}