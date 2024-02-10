using System;
using MauiViewModels.StorageModels;
using MauiViewModels.utils.Services.Certificate;

namespace MauiViewModels.FingerPrint
{
    public class FingerPrintConfirmByPinView : BaseViewModel
    {
        private readonly AccountDb _accountDb;
        private MySecureString _pin1 = new MySecureString("");
        private string _pin1Masked;
        private int _pinLength;
        private ValidationError _pin1Error = new ValidationError();
        private bool _clickIsEnabled;

        public FingerPrintConfirmByPinView(AccountDb accountDb)
        {
            _accountDb = accountDb;

            _pinLength = 4;
        }

        public override void OnAppearing()
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

        public void Cancel_OnClicked()
        {
            if (!ClickIsEnabled)
                return;
            ClickIsEnabled = false;

            _navigationService.NavigateTop();
            ClickIsEnabled = true;
        }

        public bool ClickIsEnabled
        {
            get => _clickIsEnabled;
            set
            {
                if (value == _clickIsEnabled) return;
                _clickIsEnabled = value;
                OnPropertyChanged();
            }
        }
    }
}