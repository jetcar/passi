using System.Net;
using MauiApp2.Tools;
using MauiViewModels.StorageModels;
using MauiViewModels.utils.Services;
using MauiViewModels.utils.Services.Certificate;
using Newtonsoft.Json;
using WebApiDto;
using WebApiDto.Auth;

namespace MauiApp2.utils.Services
{
    public class FingerPrintService : IFingerPrintService
    {
        private INavigationService _navigationService;
        private ISecureRepository _secureRepository;
        private ICertHelper _certHelper;
        private IRestService _restService;
        private IMainThreadService _mainThreadService;

        public FingerPrintService(INavigationService navigationService, ISecureRepository secureRepository, ICertHelper certHelper, IRestService restService, IMainThreadService mainThreadService)
        {
            _navigationService = navigationService;
            _secureRepository = secureRepository;
            _certHelper = certHelper;
            _restService = restService;
            _mainThreadService = mainThreadService;
        }

        public void StartReadingConfirmRequest(NotificationDto message, AccountDb accountDb, Action<string> errorAction)
        {
            App.FingerPrintReadingResult = (fingerPrintResult) =>
            {
                if (fingerPrintResult.ErrorMessage == null)
                {
                    _navigationService.PushModalSinglePage(new LoadingView(() =>
                    {
                        var privatecertificate = _secureRepository.GetCertificateWithFingerPrint(accountDb);

                        _certHelper.SignByFingerPrint(message.RandomString, privatecertificate).ContinueWith(signedGuid =>
                        {
                            if (signedGuid.IsFaulted)
                            {
                                _navigationService.PopModal().ContinueWith((task =>
                                {
                                    errorAction("Invalid Pin");
                                }));
                                return;
                            }

                            var authorizeDto = new AuthorizeDto
                            {
                                SignedHash = signedGuid.Result,
                                PublicCertThumbprint = _secureRepository.GetAccount(message.AccountGuid).Thumbprint,
                                SessionId = message.SessionId
                            };

                            _restService.ExecutePostAsync(accountDb.Provider, accountDb.Provider.Authorize, authorizeDto).ContinueWith((response) =>
                            {
                                if (response.Result.IsSuccessful)
                                {
                                    _mainThreadService.BeginInvokeOnMainThread(() => { _navigationService.NavigateTop(); });
                                }
                                else if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                                {
                                    _mainThreadService.BeginInvokeOnMainThread(() =>
                                    {
                                        _navigationService.PopModal().ContinueWith((task =>
                                        {
                                            errorAction(JsonConvert
                                                .DeserializeObject<ApiResponseDto<string>>(response.Result.Content)
                                                .errors);
                                        }));
                                    });
                                }
                                else
                                {
                                    _mainThreadService.BeginInvokeOnMainThread(() =>
                                    {
                                        _navigationService.PopModal().ContinueWith((s) =>
                                        {
                                            errorAction("Network error. Try again");
                                        });
                                    });
                                }
                            });
                        });
                    }));
                }
                else
                {
                    errorAction(fingerPrintResult.ErrorMessage);
                    App.StartFingerPrintReading();
                }
            };
            App.StartFingerPrintReading();
        }
    }
}