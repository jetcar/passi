using System.Net;
using System.Net.Mail;
using Android.Content;
using Android.Views.InputMethods;
using Newtonsoft.Json;
using passi_maui.Registration;
using passi_maui.Tools;
using passi_maui.utils;
using WebApiDto;
using WebApiDto.SignUp;

namespace passi_maui
{
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
                return SecureRepository.LoadProviders();
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
            var context = Platform.AppContext;
            var inputMethodManager = context.GetSystemService(Context.InputMethodService) as InputMethodManager;
            if (inputMethodManager != null)
            {
                var activity = Platform.CurrentActivity;
                var token = activity.CurrentFocus?.WindowToken;
                inputMethodManager.HideSoftInputFromWindow(token, HideSoftInputFlags.None);
                activity.Window.DecorView.ClearFocus();
            }

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

            Navigation.PushModalSinglePage(new LoadingPage(),new Dictionary<string, object>() { {"Action",new Action(() =>
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
                            Navigation.PushModalSinglePage(new RegistrationConfirmation(),new Dictionary<string, object>() { {"Account",account}});
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
            })}});
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