﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using MauiCommonServices;
using MauiViewModels.Registration;
using MauiViewModels.StorageModels;
using MauiViewModels.Tools;
using Newtonsoft.Json;
using WebApiDto;
using WebApiDto.SignUp;

namespace MauiViewModels.Main
{
    public class AddAccountViewModel : PassiBaseViewModel
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
                return _secureRepository.LoadProviders().Result;
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

        public AddAccountViewModel()
        {
            if (Debugger.IsAttached)
                EmailText = "admin@passi.cloud";

            CurrentProvider = Providers.First(x => x.IsDefault);
        }

        public void Button_OnClicked()
        {
            if (!IsValid(EmailText))
            {
                return;
            }

            var account = new AccountDb() { Email = EmailText, DeviceId = _secureRepository.GetDeviceId(), Guid = Guid.NewGuid() };
            var signupDto = new SignupDto()
            {
                Email = EmailText,
                UserGuid = account.Guid,
                DeviceId = _secureRepository.GetDeviceId()
            };

            _navigationService.PushModalSinglePage(new LoadingViewModel(new System.Action(() =>
            {
                _restService.ExecutePostAsync(CurrentProvider, CurrentProvider.SignupPath, signupDto).ContinueWith((response) =>
                {
                    var responseResult = response.Result;
                    if (!responseResult.IsSuccessful && responseResult.StatusCode == HttpStatusCode.BadRequest)
                    {
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            _navigationService.PopModal().ContinueWith((task =>
                            {
                                var responseError = JsonConvert.DeserializeObject<ApiResponseDto<string>>(responseResult.Content);
                                ResponseError = responseError.errors;
                            }));
                        });
                    }
                    else if (responseResult.IsSuccessful)
                    {
                        account.Provider = CurrentProvider;
                        _secureRepository.AddAccount(account);
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            _navigationService.PushModalSinglePage((new RegistrationConfirmationViewModel(account)
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

        public void CancelButton_OnClicked()
        {
            _navigationService.NavigateTop();
        }

        public void Picker_OnSelectedIndexChanged(int selectedIndex)
        {
            CurrentProvider = Providers[selectedIndex];
        }
    }
}