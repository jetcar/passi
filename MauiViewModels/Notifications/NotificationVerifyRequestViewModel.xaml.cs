﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Timers;
using MauiCommonServices;
using MauiViewModels.StorageModels;
using MauiViewModels.Tools;
using Microsoft.Maui.Graphics;
using Newtonsoft.Json;
using WebApiDto;
using WebApiDto.Auth;
using Color = Microsoft.Maui.Graphics.Color;
using Timer = System.Timers.Timer;

namespace MauiViewModels.Notifications
{
    public class NotificationVerifyRequestViewModel : PassiBaseViewModel
    {
        private List<WebApiDto.Auth.Color> possibleCodes = null;
        private Color _color1;
        private Color _color2;
        private Color _color3;
        private string _requesterName;
        private string _returnHost;
        private ValidationError _colorError = new ValidationError();
        private Timer _timer;
        private string _timeLeft;
        private bool _isButtonEnabled = true;
        private string _responseError;

        public NotificationVerifyRequestViewModel(NotificationDto notificationDto)
        {
            Message = notificationDto;
            UpdateTimeLeft();
            _timer = new Timer();
            _timer.Enabled = true;
            _timer.Interval = 500;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        public override void OnDisappearing(object sender, EventArgs eventArgs)
        {
            _timer.Elapsed -= _timer_Elapsed;

            _timer.Stop();
            _timer.Dispose();
            base.OnDisappearing(sender, eventArgs);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Message != null)
            {
                var leftTotalSeconds = UpdateTimeLeft();
                if (leftTotalSeconds <= 0)
                {
                    _timer.Stop();
                    _mainThreadService.BeginInvokeOnMainThread(() =>
                    {
                        _navigationService.DisplayAlert("Timeout", "Session Expired", "Ok");

                        _navigationService.NavigateTop();
                    });
                }
            }
        }

        private int UpdateTimeLeft()
        {
            var left = Message.ExpirationTime - _dateTimeService.UtcNow;
            var leftTotalSeconds = ((int)left.TotalSeconds);
            TimeLeft = leftTotalSeconds.ToString();
            return leftTotalSeconds;
        }

        public override void OnAppearing(object sender, EventArgs eventArgs)
        {
            _account = _secureRepository.GetAccount(Message.AccountGuid);
            _account.Provider = _secureRepository.GetProvider(_account.ProviderGuid);
            RequesterName = Message.Sender;
            ReturnHost = Message.ReturnHost;

            possibleCodes = new List<WebApiDto.Auth.Color>()
            {
                WebApiDto.Auth.Color.blue,
                WebApiDto.Auth.Color.green,
                WebApiDto.Auth.Color.red,
                WebApiDto.Auth.Color.yellow
            };
            var random = new Random();
            var correctIndex = random.Next(1, 3);
            possibleCodes.Remove(Message.ConfirmationColor);
            switch (correctIndex)
            {
                case 1:
                    Color1 = GetColor(Message.ConfirmationColor);
                    Color2 = RandomColor();
                    Color3 = RandomColor();
                    break;

                case 2:
                    Color1 = RandomColor();
                    Color2 = GetColor(Message.ConfirmationColor);
                    Color3 = RandomColor();
                    break;

                case 3:
                    Color1 = RandomColor();
                    Color2 = RandomColor();
                    Color3 = GetColor(Message.ConfirmationColor);
                    break;
            }

            base.OnAppearing(sender, eventArgs);
            CommonApp.CancelNotifications.Invoke();
            _secureRepository.ReleaseSessionKey(Message.SessionId);
        }

        public AccountDb _account;

        private Color RandomColor()
        {
            var random = new Random();
            var randomIndex = random.Next(0, possibleCodes.Count - 1);
            var randomColor = possibleCodes[randomIndex];
            possibleCodes.Remove(randomColor);
            return GetColor(randomColor);
        }

        private Color GetColor(WebApiDto.Auth.Color color)
        {
            switch (color)
            {
                case WebApiDto.Auth.Color.yellow:
                    return Colors.Yellow;

                case WebApiDto.Auth.Color.green:
                    return Colors.Green;

                case WebApiDto.Auth.Color.red:
                    return Colors.Red;

                case WebApiDto.Auth.Color.blue:
                    return Colors.Blue;
            }
            return Colors.Black;
        }

        public string RequesterName
        {
            get => _requesterName;
            set
            {
                _requesterName = value;
                OnPropertyChanged();
            }
        }

        public string ReturnHost
        {
            get => _returnHost;
            set
            {
                _returnHost = value;
                OnPropertyChanged();
            }
        }

        public Color Color1
        {
            get => _color1;
            set
            {
                _color1 = value;
                OnPropertyChanged();
            }
        }

        public bool IsButtonEnabled
        {
            get => _isButtonEnabled;
            set
            {
                _isButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        public Color Color2
        {
            get => _color2;
            set
            {
                _color2 = value;
                OnPropertyChanged();
            }
        }

        public Color Color3
        {
            get => _color3;
            set
            {
                _color3 = value;
                OnPropertyChanged();
            }
        }

        public NotificationDto Message { get; set; }

        public ValidationError ColorError
        {
            get => _colorError;
            set
            {
                _colorError = value;
                OnPropertyChanged();
            }
        }

        public string TimeLeft
        {
            get => _timeLeft;
            set
            {
                _timeLeft = value;
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

        public void ImageButton1_OnClicked()
        {
            if (Color1 == GetColor(Message.ConfirmationColor))
            {
                Process();
            }
            else
            {
                IsButtonEnabled = false;
                ColorError.HasError = true;
                ColorError.Text = "Invalid confirmation color. Go back and try again.";
            }
        }

        private void Process()
        {
            if (_account.pinLength == 0 && !_account.HaveFingerprint)
            {
                _navigationService.PushModalSinglePage(new LoadingViewModel(() =>
                    {
                        _certHelper.Sign(Message.AccountGuid, null, Message.RandomString).ContinueWith(signedGuid =>
                        {
                            if (signedGuid.IsFaulted)
                            {
                                _mainThreadService.BeginInvokeOnMainThread(() =>
                                {
                                    _navigationService.PopModal().ContinueWith((task =>
                                    {
                                        ResponseError = "Certificate Error:" + signedGuid.Exception.Message;
                                    }));
                                });

                                return;
                            }

                            var accountDb = _secureRepository.GetAccount(Message.AccountGuid);
                            accountDb.Provider = _secureRepository.GetProvider(accountDb.ProviderGuid);
                            var authorizeDto = new AuthorizeDto
                            {
                                SignedHash = signedGuid.Result,
                                PublicCertThumbprint = accountDb.Thumbprint,
                                SessionId = Message.SessionId
                            };
                            _restService.ExecutePostAsync(accountDb.Provider, accountDb.Provider.Authorize, authorizeDto).ContinueWith((response) =>
                            {
                                if (response.Result.IsSuccessful)
                                {
                                    _mainThreadService.BeginInvokeOnMainThread(() =>
                                    {
                                        _navigationService.NavigateTop();
                                        //CommonApp.CloseApp.Invoke();
                                    });
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
                return;
            }

            if (_account.pinLength == 0 && _account.HaveFingerprint)
            {
                _fingerPrintService.StartReadingConfirmRequest(Message, _account, (error) =>
                {
                    ResponseError = error;
                });
            }
            else
            {
                _navigationService.PushModalSinglePage(new ConfirmByPinViewModel() { Message = Message });
            }
        }

        public void ImageButton2_OnClicked()
        {
            if (Color2 == GetColor(Message.ConfirmationColor))
                Process();
            else
            {
                IsButtonEnabled = false;
                ColorError.HasError = true;
                ColorError.Text = "Invalid confirmation color. Go back and try again.";
            }
        }

        public void ImageButton3_OnClicked()
        {
            if (Color3 == GetColor(Message.ConfirmationColor))
                Process();
            else
            {
                IsButtonEnabled = false;
                ColorError.HasError = true;
                ColorError.Text = "Invalid confirmation color. Go back and try again.";
            }
        }

        public void Cancel_OnClicked()
        {
            var accountDb = _secureRepository.GetAccount(Message.AccountGuid);
            if (accountDb != null)
                accountDb.Provider = _secureRepository.GetProvider(accountDb.ProviderGuid);
            _restService.ExecuteAsync(accountDb.Provider, accountDb.Provider.CancelCheck + "?SessionId=" + Message.SessionId);
            _navigationService.NavigateTop();
        }
    }
}