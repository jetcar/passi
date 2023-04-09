using System.Net;
using System.Timers;
using AppCommon;
using Newtonsoft.Json;
using passi_maui.Tools;
using passi_maui.utils;
using passi_maui.utils.Certificate;
using WebApiDto;
using WebApiDto.Auth;
using Timer = System.Timers.Timer;

namespace passi_maui.Notifications
{
    [QueryProperty("Message", "Message")]
    public partial class ConfirmByPinView : ContentPage, IConfirmationView
    {
        private string _requesterName;
        private string _pin1 = "";
        private string _pin1Masked;
        private string _returnHost;
        private int _pinLength;
        private Timer _timer;
        private string _timeLeft;
        private string _responseError;
        private ValidationError _pin1Error = new ValidationError();
        private AccountDb _accountDb;
        private string _email;

        public ConfirmByPinView()
        {
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
                var left = Message.ExpirationTime - DateTimeService.UtcNow;
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
            _accountDb = SecureRepository.GetAccount(Message.AccountGuid);
            Email = _accountDb.Email;
            _pinLength = _accountDb.pinLength;
            if (_accountDb.HaveFingerprint)
            {
                App.StartFingerPrintReading();
                App.FingerPrintReadingResult = (fingerPrintResult) =>
                {
                    if (fingerPrintResult.ErrorMessage == null)
                    {
                        Navigation.PushModalSinglePage(new LoadingPage(),new Dictionary<string, object>() { {"Action",() =>
                        {
                            CertHelper.SignByFingerPrint(Message.AccountGuid, Message.RandomString).ContinueWith(signedGuid =>
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
                                    PublicCertThumbprint = SecureRepository.GetAccount(Message.AccountGuid).Thumbprint,
                                    SessionId = Message.SessionId
                                };

                                RestService.ExecutePostAsync(_accountDb.Provider, _accountDb.Provider.Authorize, authorizeDto).ContinueWith((response) =>
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
                        }}});
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

        public string Pin1
        {
            get => _pin1;
            set
            {
                _pin1 = value;
                Pin1Masked = new string(_pin1.ToList().Select(x => '*').ToArray());
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
                    Pin1 = Pin1.Substring(0, Pin1.Length - 1);
                return;
            }
            Pin1 += value;
            if (Pin1.Length == _pinLength)
            {
                SignRequestAndSendResponce();
            }
        }

        private void SignRequestAndSendResponce()
        {
            App.CancelfingerPrint();
            Navigation.PushModalSinglePage(new LoadingPage(),new Dictionary<string, object>() { {"Action",() =>
            {
                CertHelper.Sign(Message.AccountGuid, Pin1, Message.RandomString).ContinueWith(signedGuid =>
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
                        PublicCertThumbprint = SecureRepository.GetAccount(Message.AccountGuid).Thumbprint,
                        SessionId = Message.SessionId
                    };
                    RestService.ExecutePostAsync(_accountDb.Provider, _accountDb.Provider.Authorize, authorizeDto).ContinueWith((response) =>
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
            }}});
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

            RestService.ExecuteAsync(_accountDb.Provider, _accountDb.Provider.CancelCheck + "?SessionId=" + Message.SessionId);

            Navigation.NavigateTop();
            element.IsEnabled = true;
        }
    }
}