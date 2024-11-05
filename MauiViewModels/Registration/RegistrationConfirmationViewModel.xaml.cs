using System;
using System.Net;
using MauiViewModels.StorageModels;
using MauiViewModels.Tools;
using Newtonsoft.Json;
using WebApiDto;
using WebApiDto.SignUp;

namespace MauiViewModels.Registration
{
    public class RegistrationConfirmationViewModel : PassiBaseViewModel
    {
        private string _code = "";
        private string _responseError;
        private string _email;

        public RegistrationConfirmationViewModel(AccountDb account)
        {
            Account = account;
        }

        public override void OnAppearing(object sender, EventArgs eventArgs)
        {
            Email = Account.Email;
            base.OnAppearing(sender, eventArgs);
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
                Username = Email,
            };
            _navigationService.PushModalSinglePage(new LoadingViewModel(() =>
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
                                _navigationService.PushModalSinglePage(new FinishConfirmationViewModel()
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

        public void CancelButton_OnClicked()
        {
            _navigationService.NavigateTop();
        }
    }
}