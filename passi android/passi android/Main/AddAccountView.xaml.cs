using Newtonsoft.Json;
using passi_android.Registration;
using passi_android.StorageModels;
using passi_android.Tools;
using passi_android.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using WebApiDto;
using WebApiDto.SignUp;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Main
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddAccountView : BaseContentPage
    {
        private string _emailText = "";
        private ValidationError _emailError;
        private string _responseError;
        private ProviderDb _currentProvider;

        public string EmailText
        {
            get
            {
                return _emailText;
            }
            set
            {
                _emailText = value;
                OnPropertyChanged();
                ResponseError = "";
            }
        }

        public ValidationError EmailError
        {
            get => _emailError;
            set
            {
                _emailError = value;
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

        public List<ProviderDb> Providers
        {
            get
            {
                return _secureRepository.LoadProviders();
            }
        }

        public ProviderDb CurrentProvider
        {
            get => _currentProvider;
            set
            {
                if (Equals(value, _currentProvider)) return;
                _currentProvider = value;
                OnPropertyChanged();
            }
        }

        public AddAccountView()
        {
            if (Debugger.IsAttached)
                EmailText = "admin@passi.cloud";

            if (!App.IsTest)
                InitializeComponent();
            BindingContext = this;
            CurrentProvider = Providers.First(x => x.IsDefault);
        }

        public void Button_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            if (!IsValid(EmailText))
            {
                button.IsEnabled = true;
                return;
            }

            var account = new AccountDb() { Email = EmailText, DeviceId = _secureRepository.GetDeviceId(), Guid = Guid.NewGuid() };
            var signupDto = new SignupDto()
            {
                Email = EmailText,
                UserGuid = account.Guid,
                DeviceId = _secureRepository.GetDeviceId()
            };

            _navigationService.PushModalSinglePage(new LoadingView(new Action(() =>
            {
                _restService.ExecutePostAsync(CurrentProvider, CurrentProvider.SignupPath, signupDto).ContinueWith((response) =>
                {
                    if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                    {
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            _navigationService.PopModal().ContinueWith((task =>
                            {
                                var responseError = JsonConvert.DeserializeObject<ApiResponseDto<string>>(response.Result.Content);
                                ResponseError = responseError.errors;
                            }));
                        });
                    }
                    else if (response.Result.IsSuccessful)
                    {
                        account.Provider = CurrentProvider;
                        _secureRepository.AddAccount(account);
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            _navigationService.PushModalSinglePage((new RegistrationConfirmationView(account)
                            ));
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
            })));
            button.IsEnabled = true;
        }

        private bool IsValid(string emailText)
        {
            var isValid = false;
            try
            {
                string email = emailText;
                var mail = new MailAddress(email);
                bool isValidEmail = mail.Host.Contains(".");
                isValid = isValidEmail;
            }
            catch (Exception)
            {
                isValid = false;
            }

            EmailError = new ValidationError() { HasError = !isValid, Text = !isValid ? "invalid Email" : "" };
            return isValid;
        }

        private void CancelButton_OnClicked(object sender, EventArgs e)
        {
            _navigationService.NavigateTop();
        }

        private void Picker_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedIndex = ((Picker)sender).SelectedIndex;
            CurrentProvider = Providers[selectedIndex];
        }
    }
}