﻿using System;
using System.Linq;
using System.Net;
using System.Timers;
using AppCommon;
using Newtonsoft.Json;
using passi_android.Tools;
using passi_android.utils;
using passi_android.utils.Certificate;
using WebApiDto;
using WebApiDto.Auth;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using CertHelper = passi_android.utils.Certificate.CertHelper;

namespace passi_android.Notifications
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfirmByPinView : ContentPage, IConfirmationView
    {
        private string _requesterName;
        private MySecureString _pin1 = new MySecureString("");
        private string _pin1Masked;
        private string _returnHost;
        private int _pinLength;
        private Timer _timer;
        private string _timeLeft;
        private string _responseError;
        private ValidationError _pin1Error = new ValidationError();
        private AccountDb _accountDb;
        private string _email;
        ISecureRepository _secureRepository;
        IRestService _restService;
        IDateTimeService _dateTimeService;
        ICertHelper _certHelper;
        private INavigationService Navigation;
        public ConfirmByPinView()
        {
            _secureRepository = App.Services.GetService<ISecureRepository>();
            _restService = App.Services.GetService<IRestService>();
            _dateTimeService = App.Services.GetService<IDateTimeService>();
            _certHelper = App.Services.GetService<ICertHelper>();

            Navigation = App.Services.GetService<INavigationService>();
            InitializeComponent();
            BindingContext = this;

            _pinLength = 4;
            _timer = new Timer();
            _timer.Enabled = true;
            _timer.Interval = 500;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Message != null)
            {
                var left = Message.ExpirationTime - _dateTimeService.UtcNow;
                var leftTotalSeconds = ((int)left.TotalSeconds);
                TimeLeft = leftTotalSeconds.ToString();
                if (leftTotalSeconds <= 0)
                {
                    _timer.Stop();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Application.Current.MainPage.DisplayAlert("Timeout", "Session Expired", "Ok");

                        Navigation.NavigateTop();
                    });
                }
            }
        }

        protected override void OnDisappearing()
        {
            _timer.Elapsed -= _timer_Elapsed;

            _timer.Stop();
            _timer.Dispose();
            base.OnDisappearing();
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

        protected override void OnAppearing()
        {
            RequesterName = Message.Sender;
            ReturnHost = Message.ReturnHost;
            _accountDb = _secureRepository.GetAccount(Message.AccountGuid);
            Email = _accountDb.Email;
            _pinLength = _accountDb.pinLength;
            if (_accountDb.HaveFingerprint)
            {
                App.StartFingerPrintReading();
                App.FingerPrintReadingResult = (fingerPrintResult) =>
                {
                    if (fingerPrintResult.ErrorMessage == null)
                    {
                        Navigation.PushModalSinglePage(new LoadingPage(() =>
                        {
                            var privatecertificate = _secureRepository.GetCertificateWithFingerPrint(Message.AccountGuid);

                            _certHelper.SignByFingerPrint(Message.RandomString, privatecertificate).ContinueWith(signedGuid =>
                            {
                                if (signedGuid.IsFaulted)
                                {
                                    Navigation.PopModal().ContinueWith((task =>
                                    {
                                        Pin1Error.HasError = true;
                                        Pin1Error.Text = "Invalid Pin";
                                    }));
                                    return;
                                }

                                var authorizeDto = new AuthorizeDto
                                {
                                    SignedHash = signedGuid.Result,
                                    PublicCertThumbprint = _secureRepository.GetAccount(Message.AccountGuid).Thumbprint,
                                    SessionId = Message.SessionId
                                };

                                _restService.ExecutePostAsync(_accountDb.Provider, _accountDb.Provider.Authorize, authorizeDto).ContinueWith((response) =>
                                {
                                    if (response.Result.IsSuccessful)
                                    {
                                        MainThread.BeginInvokeOnMainThread(() => { Navigation.NavigateTop(); });
                                    }
                                    else if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                                    {
                                        MainThread.BeginInvokeOnMainThread(() =>
                                        {
                                            Navigation.PopModal().ContinueWith((task =>
                                            {
                                                ResponseError = JsonConvert
                                                    .DeserializeObject<ApiResponseDto<string>>(response.Result.Content)
                                                    .Message;
                                            }));
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
                            });
                        }));
                    }
                    else
                    {
                        ResponseError = fingerPrintResult.ErrorMessage;
                        App.StartFingerPrintReading();
                    }
                };
            }
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

        public string TimeLeft
        {
            get => _timeLeft;
            set
            {
                _timeLeft = value;
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

        public NotificationDto Message { get; set; }

        public string ReturnHost
        {
            get => _returnHost;
            set
            {
                _returnHost = value;
                OnPropertyChanged();
            }
        }

        private void NumbersPad_OnNumberClicked(string value)
        {
            if (value == "confirm")
            {
                SignRequestAndSendResponce();
                return;
            }
            if (value == "del")
            {
                if (Pin1.Length > 0)
                    Pin1 = Pin1.TrimEnd(1);
                return;
            }

            Pin1.AppendChar(value);
            if (Pin1.Length == _pinLength)
            {
                SignRequestAndSendResponce();
            }
        }

        private void SignRequestAndSendResponce()
        {
            App.CancelFingerprintReading();
            Navigation.PushModalSinglePage(new LoadingPage(() =>
            {
                _certHelper.Sign(Message.AccountGuid, Pin1, Message.RandomString).ContinueWith(signedGuid =>
                {
                    if (signedGuid.IsFaulted)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Navigation.PopModal().ContinueWith((task =>
                            {
                                Pin1Error.HasError = true;
                                Pin1Error.Text = "Invalid Pin";
                            }));
                        });

                        return;
                    }

                    var authorizeDto = new AuthorizeDto
                    {
                        SignedHash = signedGuid.Result,
                        PublicCertThumbprint = _secureRepository.GetAccount(Message.AccountGuid).Thumbprint,
                        SessionId = Message.SessionId
                    };
                    _restService.ExecutePostAsync(_accountDb.Provider, _accountDb.Provider.Authorize, authorizeDto).ContinueWith((response) =>
                    {
                        if (response.Result.IsSuccessful)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                App.CloseApp.Invoke();
                                //Navigation.NavigateTop();
                            });
                        }
                        else if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Navigation.PopModal().ContinueWith((task =>
                                {
                                    ResponseError = JsonConvert
                                        .DeserializeObject<ApiResponseDto<string>>(response.Result.Content).Message;
                                }));
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
                });
            }));
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

        private void Cancel_OnClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            _restService.ExecuteAsync(_accountDb.Provider, _accountDb.Provider.CancelCheck + "?SessionId=" + Message.SessionId);

            Navigation.NavigateTop();
            element.IsEnabled = true;
        }
    }
}