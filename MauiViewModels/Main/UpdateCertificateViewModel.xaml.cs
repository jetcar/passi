using MauiViewModels.StorageModels;
using MauiViewModels.utils.Services.Certificate;
using Microsoft.Maui.Graphics;

namespace MauiViewModels.Main
{
    public class UpdateCertificateViewModel : BaseViewModel
    {
        private string _pin1Masked;
        private string _pin2Masked;
        private string _pinOldMasked;
        private MySecureString _pin1 = new MySecureString("");
        private MySecureString _pin2 = new MySecureString("");
        private MySecureString _pinOld = new MySecureString("");
        private Microsoft.Maui.Graphics.Color _pin1Color;
        private Microsoft.Maui.Graphics.Color _pin2Color;
        private Microsoft.Maui.Graphics.Color _pinOldColor;
        private string _responseError;
        private ValidationError _pin1Error = new ValidationError();
        private ValidationError _pin2Error = new ValidationError();
        private ValidationError _pinOldError = new ValidationError();

        private readonly int MinPinLenght = 4;

        public UpdateCertificateViewModel()
        {
            SetTextBox(0);
        }

        public string Pin1Masked
        {
            get => _pin1Masked;
            set
            {
                _pin1Masked = value;
                if (Pin1.Length > MinPinLenght)
                {
                    Pin1Error.HasError = false;
                    Pin1Error.Text = "";
                }
                OnPropertyChanged();
            }
        }

        public string PinOldMasked
        {
            get => _pinOldMasked;
            set
            {
                _pinOldMasked = value;
                if (PinOld.Length > MinPinLenght)
                {
                    PinOldError.HasError = false;
                    PinOldError.Text = "";
                }
                OnPropertyChanged();
            }
        }

        public string Pin2Masked
        {
            get => _pin2Masked;
            set
            {
                _pin2Masked = value;

                if (Pin1.Equals(Pin2))
                {
                    Pin2Error.HasError = false;
                    Pin2Error.Text = "";
                }
                OnPropertyChanged();
            }
        }

        public MySecureString Pin1
        {
            get => _pin1;
            set
            {
                _pin1 = value;
                Pin1Masked = _pin1.GetMasked("*");
            }
        }

        public MySecureString PinOld
        {
            get => _pinOld;
            set
            {
                _pinOld = value;
                PinOldMasked = _pinOld.GetMasked("*");
            }
        }

        public MySecureString Pin2
        {
            get => _pin2;
            set
            {
                _pin2 = value;
                Pin2Masked = _pin2.GetMasked("*");
            }
        }

        private int currentfieldIndex = 0;

        public void SetTextBox(int i)
        {
            currentfieldIndex = i;
            if (i == 0)
            {
                PinOldColor = Colors.Aquamarine;
                Pin1Color = Microsoft.Maui.Graphics.Color.FromArgb("#FAFAFA");
                Pin2Color = Microsoft.Maui.Graphics.Color.FromArgb("#FAFAFA");
            }
            if (i == 1)
            {
                Pin1Color = Colors.Aquamarine;
                Pin2Color = Microsoft.Maui.Graphics.Color.FromArgb("#FAFAFA");
                PinOldColor = Microsoft.Maui.Graphics.Color.FromArgb("#FAFAFA");
            }
            if (i == 2)
            {
                Pin2Color = Colors.Aquamarine;
                Pin1Color = Microsoft.Maui.Graphics.Color.FromArgb("#FAFAFA");
                PinOldColor = Microsoft.Maui.Graphics.Color.FromArgb("#FAFAFA");
            }
        }

        public Microsoft.Maui.Graphics.Color Pin1Color
        {
            get => _pin1Color;
            set
            {
                _pin1Color = value;
                OnPropertyChanged();
            }
        }

        public Microsoft.Maui.Graphics.Color PinOldColor
        {
            get => _pinOldColor;
            set
            {
                _pinOldColor = value;
                OnPropertyChanged();
            }
        }

        public Microsoft.Maui.Graphics.Color Pin2Color
        {
            get => _pin2Color;
            set
            {
                _pin2Color = value;
                OnPropertyChanged();
            }
        }

        public string ResponseError
        {
            get => _responseError;
            set
            {
                _responseError = value;
                OnPropertyChanged();
            }
        }

        public ValidationError Pin1Error
        {
            get => _pin1Error;
            set => _pin1Error = value;
        }

        public ValidationError Pin2Error
        {
            get => _pin2Error;
            set => _pin2Error = value;
        }

        public ValidationError PinOldError
        {
            get => _pinOldError;
            set => _pinOldError = value;
        }

        public AccountDb Account { get; set; }

        public void NumbersPad_OnNumberClicked(string value)
        {
            if (value == "confirm")
            {
                if (currentfieldIndex == 2)
                {
                    ResponseError = "";
                    PinOldError.Clear();
                    Pin1Error.Clear();
                    Pin2Error.Clear();

                    if (Pin1.Length < MinPinLenght)
                    {
                        SetTextBox(1);
                        //error
                        Pin1Error.HasError = true;
                        Pin1Error.Text = $"Pin1 should be min {MinPinLenght} numbers";

                        return;
                    }

                    if (!Pin1.Equals(Pin2))
                    {
                        SetTextBox(2);
                        //error
                        Pin2Error.HasError = true;
                        Pin2Error.Text = "Pin2 doesn't match with Pin1";

                        return;
                    }

                    _certificatesService.UpdateCertificate(Pin1, PinOld, Account, (error) =>
                    {
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            _navigationService.PopModal().ContinueWith((task =>
                            {
                                if (error == "Invalid old pin")
                                {
                                    PinOldError.HasError = true;
                                    PinOldError.Text = "Invalid old pin";

                                    SetTextBox(0);
                                }
                                else
                                {
                                    ResponseError = error;
                                }
                            }));
                        });
                    });
                    return;
                }
                if (currentfieldIndex == 0)
                {
                    SetTextBox(1);
                    return;
                }
                if (currentfieldIndex == 1)
                {
                    SetTextBox(2);
                }
                return;
            }
            if (value == "del")
            {
                if (currentfieldIndex == 0)
                    if (PinOld.Length > 0)
                        PinOld = PinOld.TrimEnd(1);
                if (currentfieldIndex == 1)
                    if (Pin1.Length > 0)
                        Pin1 = Pin1.TrimEnd(1);
                if (currentfieldIndex == 2)
                    if (Pin2.Length > 0)
                        Pin2 = Pin2.TrimEnd(1);
                return;
            }

            if (currentfieldIndex == 0)
            {
                PinOld.AppendChar(value);
                PinOldMasked = PinOld.GetMasked("*");
            }

            if (currentfieldIndex == 1)
            {
                Pin1.AppendChar(value);
                Pin1Masked = Pin1.GetMasked("*");
            }

            if (currentfieldIndex == 2)
            {
                Pin2.AppendChar(value);
                Pin2Masked = Pin2.GetMasked("*");
            }
        }

        public void ClearPin1_OnClicked()
        {
            Pin1 = new MySecureString("");
            SetTextBox(1);
        }

        public void ClearPin2_OnClicked()
        {
            Pin2 = new MySecureString("");
            SetTextBox(2);
        }

        public void ClearPinOld_OnClicked()
        {
            PinOld = new MySecureString("");
            SetTextBox(0);
        }

        public void Button_Cancel()
        {
            _navigationService.PopModal();
        }
    }
}