using System;
using System.Linq;
using passi_android.Tools;
using passi_android.utils;
using passi_android.utils.Services;
using passi_android.utils.Services.Certificate;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.FingerPrint
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FingerPrintConfirmByPinView : BaseContentPage
    {
        private readonly AccountDb _accountDb;
        private MySecureString _pin1 = new MySecureString("");
        private string _pin1Masked;
        private int _pinLength;
        private ValidationError _pin1Error = new ValidationError();

        public FingerPrintConfirmByPinView(AccountDb accountDb)
        {
            _accountDb = accountDb;
            if (!App.IsTest)
                InitializeComponent();
            BindingContext = this;

            _pinLength = 4;
        }

        protected override void OnAppearing()
        {
            _pinLength = _accountDb.pinLength;
            base.OnAppearing();
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
            if (value == "del")
            {
                if (Pin1.Length > 0)
                    Pin1 = Pin1.TrimEnd(1);
                return;
            }

            Pin1.AppendChar(value);
            Pin1Masked = Pin1.GetMasked("*");
            if (Pin1.Length == _pinLength || value == "confirm")
            {
                _certificatesService.CreateFingerPrintCertificate(_accountDb, Pin1, (error) =>
                {
                    if (error != null)
                    {
                        Pin1Error.HasError = true;
                        Pin1Error.Text = error;
                    }
                });

            }
        }

        private void Cancel_OnClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            _navigationService.NavigateTop();
            element.IsEnabled = true;
        }
    }
}