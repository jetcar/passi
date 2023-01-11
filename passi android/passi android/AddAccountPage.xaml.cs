using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using AppCommon;
using AppConfig;
using Newtonsoft.Json;
using passi_android.Menu;
using passi_android.Registration;
using passi_android.Tools;
using passi_android.utils;
using RestSharp;
using WebApiDto;
using WebApiDto.SignUp;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddAccountPage : ContentPage
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
                OnPropertyChanged();
            }
        }

        public List<ProviderDb> Providers
        {
            get
            {
                return MainPage.Providers;
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

        public AddAccountPage()
        {
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

            var account = new AccountDb() { Email = EmailText, DeviceId = SecureRepository.GetDeviceId(), Guid = Guid.NewGuid() };
            var signupDto = new SignupDto()
            {
                Email = EmailText,
                UserGuid = account.Guid,
                DeviceId = SecureRepository.GetDeviceId()
            };

            Navigation.PushModalSinglePage(new LoadingPage(new Action(() =>
            {
                RestService.ExecutePostAsync(CurrentProvider, CurrentProvider.SignupPath, signupDto).ContinueWith((response) =>
                {
                    if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Navigation.PopModal().ContinueWith((task =>
                            {
                                var responseError = JsonConvert.DeserializeObject<ApiResponseDto<string>>(response.Result.Content);
                                ResponseError = responseError.Message;
                            }));
                        });
                    }
                    else if (response.Result.IsSuccessful)
                    {
                        account.Provider = CurrentProvider;
                        SecureRepository.AddAccount(account);
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Navigation.PushModalSinglePage(new NavigationPage(new RegistrationConfirmation()
                            { Account = account, CurrentProvider = CurrentProvider }));
                        });
                    }
                    else
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Navigation.PopModal().ContinueWith((s) =>
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
            Navigation.NavigateTop();
        }

        private void Picker_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedIndex = ((Picker)sender).SelectedIndex;
            CurrentProvider = Providers[selectedIndex];
        }
    }
}