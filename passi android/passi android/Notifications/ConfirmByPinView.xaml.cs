using System;
using System.Linq;
using System.Net;
using System.Timers;
using AppCommon;
using Newtonsoft.Json;
using passi_android.Tools;
using passi_android.utils;
using passi_android.utils.Services;
using passi_android.utils.Services.Certificate;
using WebApiDto;
using WebApiDto.Auth;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using CertHelper = passi_android.utils.Services.Certificate.CertHelper;

namespace passi_android.Notifications
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfirmByPinView : ContentPage
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
        private INavigationService _navigationService;
        private IMainThreadService _mainThreadService;
        private IFingerPrintService _fingerPrintService;
        public ConfirmByPinView()
        {
            _secureRepository = App.Services.GetService<ISecureRepository>();
            _restService = App.Services.GetService<IRestService>();
            _dateTimeService = App.Services.GetService<IDateTimeService>();
            _certHelper = App.Services.GetService<ICertHelper>();
            _fingerPrintService = App.Services.GetService<IFingerPrintService>();

            _mainThreadService = App.Services.GetService<IMainThreadService>();
            _navigationService = App.Services.GetService<INavigationService>();
            if (!App.IsTest)
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
                    _mainThreadService.BeginInvokeOnMainThread(() =>
                    {
                        DisplayAlert("Timeout", "Session Expired", "Ok");

                        _navigationService.NavigateTop();
                    });
                }
            }
        }

        protected override void OnDisappearing()
        {
            App.FingerPrintReadingResult = null;
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
                _fingerPrintService.StartReadingConfirmRequest(Message,_accountDb, (error) =>
                {
                    ResponseError = error;
                });

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

        public void NumbersPad_OnNumberClicked(string value)
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
            _navigationService.PushModalSinglePage(new LoadingView(() =>
            {
                _certHelper.Sign(Message.AccountGuid, Pin1, Message.RandomString).ContinueWith(signedGuid =>
                {
                    if (signedGuid.IsFaulted)
                    {
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            _navigationService.PopModal().ContinueWith((task =>
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
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.NavigateTop();
                                App.CloseApp.Invoke();
                            });
                        }
                        else if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                        {
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PopModal().ContinueWith((task =>
                                {
                                    ResponseError = JsonConvert
                                        .DeserializeObject<ApiResponseDto<string>>(response.Result.Content).Message;
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

        public string ResponseError
        {
            get => _responseError;
            set
            {
                _responseError = value;
                OnPropertyChanged();
            }
        }

        public void Cancel_OnClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            _restService.ExecuteAsync(_accountDb.Provider, _accountDb.Provider.CancelCheck + "?SessionId=" + Message.SessionId);

            _navigationService.NavigateTop();
            element.IsEnabled = true;
        }
    }
}