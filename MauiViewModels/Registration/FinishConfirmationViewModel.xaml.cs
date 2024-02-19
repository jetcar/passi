using System;
using System.Net;
using System.Threading.Tasks;
using MauiViewModels.StorageModels;
using MauiViewModels.Tools;
using MauiViewModels.utils.Services.Certificate;
using Microsoft.Maui.Graphics;
using Newtonsoft.Json;
using WebApiDto;
using WebApiDto.SignUp;

namespace MauiViewModels.Registration
{
    public class FinishConfirmationViewModel : BaseViewModel
    {
        private string _pin1Masked;
        private string _pin2Masked;
        private MySecureString _pin1 = new MySecureString("");
        private MySecureString _pin2 = new MySecureString("");
        private Color _pin1Color;
        private Color _pin2Color;
        private bool _secondPin;
        private string _responseError;
        private ValidationError _pin1Error = new ValidationError();
        private ValidationError _pin2Error = new ValidationError();

        private readonly int MinPinLenght = 4;

        public FinishConfirmationViewModel()
        {
            SecondPin = false;
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

        public MySecureString Pin2
        {
            get => _pin2;
            set
            {
                _pin2 = value;
                Pin2Masked = _pin2.GetMasked("*");
            }
        }

        public override void OnAppearing(object sender, EventArgs eventArgs)
        {
            EmailText = Account.Email;
            base.OnAppearing(sender, eventArgs);
        }

        private void Confirm(string code, string email, MySecureString pin)
        {
            _navigationService.PushModalSinglePage(new LoadingViewModel(() =>
            {
                GenerateCert(Account.Email, pin).ContinueWith(x =>
                {
                    var signupConfirmationDto = new SignupConfirmationDto()
                    {
                        Code = code,
                        PublicCert = x.Result.PublicCertBinary,
                        Email = email,
                        Guid = x.Result.Guid.ToString(),
                        DeviceId = _secureRepository.GetDeviceId()
                    };

                    _restService.ExecutePostAsync(Account.Provider, Account.Provider.SignupConfirmation, signupConfirmationDto).ContinueWith((response) =>
                    {
                        if (response.Result.IsSuccessful)
                        {
                            _secureRepository.UpdateAccount(x.Result);
                            _mainThreadService.BeginInvokeOnMainThread(() => { _navigationService.NavigateTop(); });
                        }
                        else if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                        {
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PopModal().ContinueWith((task =>
                                {
                                    ResponseError = JsonConvert
                                        .DeserializeObject<ApiResponseDto<string>>(response.Result.Content).errors;
                                }));
                            });
                        }
                        else
                        {
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PopModal().ContinueWith((s) =>
                                {
                                    ResponseError = "Network error. Try again";
                                });
                            });
                        }
                    });
                });
            }));
        }

        public string Code { get; set; }
        public string EmailText { get; set; }

        private async Task<AccountDb> GenerateCert(string email, MySecureString pin)
        {
            return await _certificatesService.GenerateCertificate(email, pin).ContinueWith(certificate =>
             {
                 Account.Salt = certificate.Result.Item2;
                 var certificateBytes = Convert.ToBase64String(certificate.Result.Item3); //importable certificate
                 Account.PrivateCertBinary = certificateBytes;
                 Account.pinLength = pin?.Length ?? 0;
                 var publicCertHelper = _certHelper.ConvertToPublicCertificate(certificate.Result.Item1);
                 Account.Thumbprint = publicCertHelper.Thumbprint;
                 Account.ValidFrom = publicCertHelper.NotBefore.Value;
                 Account.ValidTo = publicCertHelper.NotAfter.Value;
                 Account.PublicCertBinary = publicCertHelper.BinaryData;
                 return Account;
             });
        }

        public bool SecondPin
        {
            get => _secondPin;
            set
            {
                _secondPin = value;
                if (!value)
                {
                    Pin1Color = Colors.Aquamarine;
                    Pin2Color = Microsoft.Maui.Graphics.Color.FromArgb("#FAFAFA");
                }
                else
                {
                    Pin2Color = Colors.Aquamarine;
                    Pin1Color = Microsoft.Maui.Graphics.Color.FromArgb("#FAFAFA");
                }
            }
        }

        public Color Pin1Color
        {
            get => _pin1Color;
            set
            {
                _pin1Color = value;
                OnPropertyChanged();
            }
        }

        public Color Pin2Color
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

        public AccountDb Account { get; set; }

        public void NumbersPad_OnNumberClicked(string value)
        {
            if (value == "confirm")
            {
                if (SecondPin)
                {
                    ResponseError = "";

                    if (Pin1.Length < MinPinLenght)
                    {
                        SecondPin = false;
                        //error
                        Pin1Error.HasError = true;
                        Pin1Error.Text = $"Pin1 should be min {MinPinLenght} numbers";

                        return;
                    }

                    if (!Pin1.Equals(Pin2))
                    {
                        SecondPin = false;
                        //error
                        Pin2Error.HasError = true;
                        Pin2Error.Text = "Pin2 doesn't match with Pin1";

                        return;
                    }

                    Confirm(Code, EmailText, Pin1);
                }
                if (!SecondPin)
                {
                    SecondPin = true;
                }
                return;
            }
            if (value == "del")
            {
                if (!SecondPin)
                    if (Pin1.Length > 0)
                    {
                        Pin1 = Pin1.TrimEnd(1);
                    }
                if (SecondPin)
                    if (Pin2.Length > 0)
                    {
                        Pin2 = Pin2.TrimEnd(1);
                    }
                return;
            }

            if (!SecondPin)
            {
                Pin1.AppendChar(value);
                Pin1Masked = _pin1.GetMasked("*");
            }

            if (SecondPin)
            {
                Pin2.AppendChar(value);
                Pin2Masked = _pin2.GetMasked("*");
            }
        }

        public void ClearPin1_OnClicked()
        {
            Pin1 = new MySecureString("");
            SecondPin = false;
        }

        public void ClearPin2_OnClicked()
        {
            Pin2 = new MySecureString("");
            SecondPin = true;
        }

        public void SkipButton_OnClicked()
        {
            Confirm(Code, EmailText, null);
        }
    }
}