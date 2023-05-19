using System;
using System.Linq;
using passi_android.Tools;
using passi_android.utils;
using passi_android.utils.Certificate;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.FingerPrint
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FingerPrintConfirmByPinView : ContentPage
    {
        private readonly AccountDb _accountDb;
        private MySecureString _pin1 = new MySecureString("");
        private string _pin1Masked;
        private int _pinLength;
        private ValidationError _pin1Error = new ValidationError();
        ICertHelper _certHelper;
        INavigationService Navigation;
        public FingerPrintConfirmByPinView(AccountDb accountDb)
        {
            _certHelper = App.Services.GetService<ICertHelper>();
            _accountDb = accountDb;
            Navigation = App.Services.GetService<INavigationService>();
            InitializeComponent();
            BindingContext = this;

            _pinLength = 4;
        }

        protected override void OnAppearing()
        {
            _pinLength = _accountDb.pinLength;
        }

        public MySecureString Pin1
        {
            get => _pin1;
            set
            {
                _pin1 = value;
                Pin1Masked = _pin1.GetMasked("*");
                Pin1Error.HasError = false;
                Pin1Error.Text = "";
            }
        }

        public ValidationError Pin1Error
        {
            get => _pin1Error;
            set => _pin1Error = value;
        }

        public string Pin1Masked
        {
            get => _pin1Masked;
            set
            {
                _pin1Masked = value;
                OnPropertyChanged();
            }
        }

        public void NumbersPad_OnNumberClicked(string value)
        {
            if (value == "confirm")
            {
                SignRequestAndSendResponce(_accountDb, Pin1, (error) =>
                {
                    if (error != null)
                    {
                        Pin1Error.HasError = true;
                        Pin1Error.Text = error;
                    }
                });
                return;
            }
            if (value == "del")
            {
                if (Pin1.Length > 0)
                    Pin1 = Pin1.TrimEnd(1);
                return;
            }

            Pin1.AppendChar(value);
            if (Pin1.Length == _pinLength)
            {
                SignRequestAndSendResponce(_accountDb, Pin1,  (error) =>
                {
                    if (error != null)
                    {
                        Pin1Error.HasError = true;
                        Pin1Error.Text = error;
                    }
                });

            }
        }

        public static void SignRequestAndSendResponce(AccountDb _accountDb, MySecureString Pin1,  Action<string> callback)
        {
            var _secureRepository = App.Services.GetService<ISecureRepository>();
            INavigationService Navigation;
            Navigation = App.Services.GetService<INavigationService>();

            Navigation.PushModalSinglePage(new LoadingPage(() =>
            {
                
                _secureRepository.AddfingerPrintKey(_accountDb, Pin1).ContinueWith((result) =>
                {
                    if (result.IsFaulted)
                    {
                        callback.Invoke("Invalid Pin");
                        return;
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Application.Current.MainPage.DisplayAlert("Fingerprint", "Fingerprint key added", "Ok");
                        Navigation.NavigateTop();

                    });
                });
            }));

        }

        private void Cancel_OnClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            Navigation.NavigateTop();
            element.IsEnabled = true;
        }
    }
}