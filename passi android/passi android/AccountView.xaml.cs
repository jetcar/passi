using System;
using passi_android.FingerPrint;
using passi_android.utils;
using passi_android.utils.Services;
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
        private string _providerName;

        private INavigationService _navigationService;
        private string _message;
        private IMainThreadService _mainThreadService;

        public AccountView(AccountDb accountDb)
        {
            AccountDb = accountDb;
            _navigationService = App.Services.GetService<INavigationService>();
            _mainThreadService = App.Services.GetService<IMainThreadService>();
            if (!App.IsTest)
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
                ProviderName = AccountDb.Provider?.Name;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
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

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        private void UpdateCertificate_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;
            if (AccountDb.pinLength > 0)
                _navigationService.PushModalSinglePage(new UpdateCertificateView() { Account = AccountDb });
            else
            {
                UpdateCertificateView.StartCertGeneration(null, null, AccountDb, (error) =>
                {

                });
            }
            button.IsEnabled = true;
        }

        public void AddBiometric_Button_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;


            App.FingerPrintReadingResult = (result) =>
            {
                if (result.ErrorMessage == null)
                {
                    if (AccountDb.pinLength > 0)
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            _navigationService.PushModalSinglePage(new FingerPrintConfirmByPinView(AccountDb));
                        });
                    else
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            FingerPrintConfirmByPinView.SignRequestAndSendResponce(AccountDb, null,
                                (error) =>
                                {
                                    Message = error;
                                });
                        });
                }
                else
                {
                    Message = result.ErrorMessage;
                    App.StartFingerPrintReading();
                }
            };

            App.StartFingerPrintReading();
            
            button.IsEnabled = true;
        }

        protected override void OnDisappearing()
        {
            App.FingerPrintReadingResult = null;
            base.OnDisappearing();
        }

        private void Button_Back(object sender, EventArgs e)
        {
            _navigationService.PopModal();
        }
    }
}