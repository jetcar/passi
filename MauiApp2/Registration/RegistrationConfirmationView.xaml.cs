using System.Net;
using MauiApp2.StorageModels;
using MauiApp2.Tools;
using Newtonsoft.Json;
using WebApiDto;
using WebApiDto.SignUp;

namespace MauiApp2.Registration
{
    public partial class RegistrationConfirmationView : BaseContentPage
    {
        private string _code = "";
        private string _responseError;
        private string _email;

        public RegistrationConfirmationView(AccountDb account)
        {
            Account = account;
            if (!App.IsTest)
                InitializeComponent();
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            Email = Account.Email;
            base.OnAppearing();
        }

        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanging();
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

        public AccountDb Account { get; set; }

        public void NumbersPad_OnNumberClicked(string value)
        {
            if (value == "confirm")
            {
                CheckCodeAndNavigateForward();
                return;
            }
            if (value == "del")
            {
                if (Code.Length > 0)
                    Code = Code.Substring(0, Code.Length - 1);
                return;
            }
            Code += value;
            if (Code.Length >= 6)
            {
                CheckCodeAndNavigateForward();
            }
        }

        private void CheckCodeAndNavigateForward()
        {
            ResponseError = "";
            var signupConfirmationDto = new SignupCheckDto()
            {
                Code = Code,
                Email = Email,
            };
            _navigationService.PushModalSinglePage(new LoadingView(() =>
            {
                _restService.ExecutePostAsync(Account.Provider, Account.Provider.SignupCheck, signupConfirmationDto)
                    .ContinueWith((response) =>
                    {
                        if (response.Result.IsSuccessful)
                        {
                            Account.IsConfirmed = true;
                            Account.Provider = Account.Provider;
                            _secureRepository.UpdateAccount(Account);
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PushModalSinglePage(new FinishConfirmationView()
                                { Code = Code, Account = Account });
                            });
                        }
                        else if (!response.Result.IsSuccessful &&
                                 response.Result.StatusCode == HttpStatusCode.BadRequest)
                        {
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PopModal().ContinueWith((task =>
                                {
                                    var resultContent = response.Result.Content ?? "{\"Message\":\"Network error. Try again\"}";
                                    ResponseError = JsonConvert
                                        .DeserializeObject<ApiResponseDto>(resultContent).errors;
                                    Code = "";
                                }));
                            });
                        }
                        else
                        {
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PopModal().ContinueWith((task =>
                                {
                                    ResponseError = "Network error. Try again";
                                }));
                            });
                        }
                    });
            }));
        }

        public void CancelButton_OnClicked(object sender, EventArgs e)
        {
            _navigationService.NavigateTop();
        }
    }
}