using System;
using System.Linq;
using AppCommon;
using passi_android.Tools;
using passi_android.utils;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.FingerPrint
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FingerPrintConfirmByPinView : ContentPage
    {
        private readonly AccountDb _accountDb;
        private string _pin1 = "";
        private string _pin1Masked;
        private int _pinLength;
        private ValidationError _pin1Error = new ValidationError();

        public FingerPrintConfirmByPinView(AccountDb accountDb)
        {
            _accountDb = accountDb;
            InitializeComponent();
            BindingContext = this;

            _pinLength = 4;
        }

        protected override void OnAppearing()
        {
            _pinLength = _accountDb.pinLength;
        }

        public string Pin1
        {
            get => _pin1;
            set
            {
                _pin1 = value;
                Pin1Masked = new string(_pin1.ToList().Select(x => '*').ToArray());
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
                SignRequestAndSendResponce();
                return;
            }
            if (value == "del")
            {
                if (Pin1.Length > 0)
                    Pin1 = Pin1.Substring(0, Pin1.Length - 1);
                return;
            }
            Pin1 += value;
            if (Pin1.Length == _pinLength)
            {
                SignRequestAndSendResponce();
            }
        }

        private void SignRequestAndSendResponce()
        {
            Navigation.PushModalSinglePage(new LoadingPage(() =>
            {
                SecureRepository.AddfingerPrintKey(_accountDb.Guid, Pin1).ContinueWith((result) =>
                {
                    if (result.IsFaulted)
                    {
                        Pin1Error.HasError = true;
                        Pin1Error.Text = "Invalid Pin";
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