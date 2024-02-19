using System;
using System.IO;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MauiViewModels.StorageModels;
using MauiViewModels.Tools;
using Newtonsoft.Json;
using WebApiDto;
using WebApiDto.Certificate;

namespace MauiViewModels.utils.Services.Certificate;

public class CertificatesService : ICertificatesService
{
    private INavigationService _navigationService;
    private ISecureRepository _secureRepository;
    private IMainThreadService _mainThreadService;
    private IRestService _restService;
    private ICertHelper _certHelper;

    public CertificatesService(INavigationService navigationService, ISecureRepository secureRepository, IMainThreadService mainThreadService, IRestService restService, ICertHelper certHelper)
    {
        _navigationService = navigationService;
        _secureRepository = secureRepository;
        _mainThreadService = mainThreadService;
        _restService = restService;
        _certHelper = certHelper;
    }

    public async Task<Tuple<X509Certificate2, string, byte[]>> GenerateCertificate(string subject, MySecureString pin)
    {
        var rsa = RSA.Create(); // generate asymmetric key pair
        var req = new CertificateRequest($"cn={subject.Replace("@", "")}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        MySecureString password = new MySecureString(Guid.NewGuid().ToString());
        MySecureString fullPassword = new MySecureString(password);
        if (pin != null)
            fullPassword.Append(pin);

        var rawBytes = cert.Export(X509ContentType.Pkcs12, fullPassword.SecureStringToString());
        var certificate = new X509Certificate2(rawBytes, fullPassword.SecureStringToString(), X509KeyStorageFlags.Exportable);
        var result = new Tuple<X509Certificate2, string, byte[]>(certificate, password.SecureStringToString(), rawBytes);
        return result;
    }

    public void CreateFingerPrintCertificate(AccountDb accountDb, MySecureString pin1, Action<string> errorCallback)
    {
        _navigationService.PushModalSinglePage(new LoadingViewModel(() =>
        {
            try
            {
                var cert = _secureRepository.GetCertificateWithKey(pin1, accountDb);
                _secureRepository.SaveFingerPrintKey(accountDb, cert).ContinueWith((result) =>
                {
                    _mainThreadService.BeginInvokeOnMainThread(() =>
                    {
                        _navigationService.DisplayAlert("Fingerprint", "Fingerprint key added", "Ok");
                        _navigationService.NavigateTop();
                    });
                });
            }
            catch (Exception e)
            {
                _mainThreadService.BeginInvokeOnMainThread(() =>
                {
                    _navigationService.PopModal().ContinueWith((task =>
                    {
                        errorCallback.Invoke("Invalid Pin");
                    }));
                });
            }
        }));
    }

    public void UpdateCertificate(MySecureString pinNew, MySecureString pinOld, AccountDb accountDb, Action<string> errorAction)
    {
        _navigationService.PushModalSinglePage(new LoadingViewModel(() =>
        {
            GenerateCertFromOldCertificate(pinNew, pinOld, accountDb, errorAction).ContinueWith(certDto =>
            {
                if (certDto?.Result != null)
                {
                    var provider = _secureRepository.GetProvider(accountDb.ProviderGuid);
                    certDto.Result.Item1.DeviceId = _secureRepository.GetDeviceId();
                    _restService.ExecutePostAsync(provider, provider.UpdateCertificate, certDto.Result.Item1).ContinueWith((response) =>
                    {
                        if (response.Result.IsSuccessful)
                        {
                            var certificateBase64 = Convert.ToBase64String(certDto?.Result.Item4); //importable certificate

                            var publicCertHelper = _certHelper.ConvertToPublicCertificate(certDto.Result.Item2);

                            accountDb.Salt = certDto.Result.Item3;
                            accountDb.PrivateCertBinary = certificateBase64;
                            accountDb.pinLength = pinNew?.Length ?? 0;
                            accountDb.Thumbprint = certDto.Result.Item2.Thumbprint;
                            accountDb.ValidFrom = certDto.Result.Item2.NotBefore;
                            accountDb.ValidTo = certDto.Result.Item2.NotAfter;
                            accountDb.PublicCertBinary = publicCertHelper.BinaryData;

                            _secureRepository.UpdateAccount(accountDb);
                            if (accountDb.HaveFingerprint)
                                _secureRepository.SaveFingerPrintKey(accountDb, certDto.Result.Item2).GetAwaiter().GetResult();

                            _mainThreadService.BeginInvokeOnMainThread(() => { _navigationService.NavigateTop(); });
                        }
                        else if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                        {
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PopModal().ContinueWith((task) =>
                                {
                                    errorAction.Invoke(JsonConvert
                                        .DeserializeObject<ApiResponseDto<string>>(response.Result.Content)
                                        .errors);
                                });
                            });
                        }
                        else
                        {
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PopModal().ContinueWith((task) =>
                                {
                                    errorAction.Invoke("Network error. Try again");
                                });
                            });
                        }
                    });
                }
            });
        }));
    }

    public void UpdateCertificateFingerprint(AccountDb accountDb, Action<string> errorAction)
    {
        _navigationService.PushModalSinglePage(new LoadingViewModel(() =>
        {
            GenerateCertFromOldCertificateFingerPrint(accountDb, errorAction).ContinueWith(certDto =>
            {
                if (certDto?.Result != null)
                {
                    var provider = _secureRepository.GetProvider(accountDb.ProviderGuid);
                    certDto.Result.Item1.DeviceId = _secureRepository.GetDeviceId();
                    _restService.ExecutePostAsync(provider, provider.UpdateCertificate, certDto.Result.Item1).ContinueWith((response) =>
                    {
                        if (response.Result.IsSuccessful)
                        {
                            var certificateBase64 = Convert.ToBase64String(certDto?.Result.Item4); //importable certificate

                            var publicCertHelper = _certHelper.ConvertToPublicCertificate(certDto.Result.Item2);
                            accountDb.Salt = certDto.Result.Item3;
                            accountDb.PrivateCertBinary = certificateBase64;
                            accountDb.Thumbprint = certDto.Result.Item2.Thumbprint;
                            accountDb.ValidFrom = certDto.Result.Item2.NotBefore;
                            accountDb.ValidTo = certDto.Result.Item2.NotAfter;
                            accountDb.PublicCertBinary = publicCertHelper.BinaryData;

                            _secureRepository.UpdateAccount(accountDb);
                            _secureRepository.SaveFingerPrintKey(accountDb, certDto.Result.Item2).GetAwaiter().GetResult();

                            _mainThreadService.BeginInvokeOnMainThread(() => { _navigationService.NavigateTop(); });
                        }
                        else if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                        {
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PopModal().ContinueWith((task) =>
                                {
                                    errorAction.Invoke(JsonConvert
                                        .DeserializeObject<ApiResponseDto<string>>(response.Result.Content)
                                        .errors);
                                });
                            });
                        }
                        else
                        {
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PopModal().ContinueWith((task) =>
                                {
                                    errorAction.Invoke("Network error. Try again");
                                });
                            });
                        }
                    });
                }
            });
        }));
    }

    private async Task<Tuple<CertificateUpdateDto, X509Certificate2, string, byte[]>> GenerateCertFromOldCertificate(MySecureString pinNew, MySecureString pinOld, AccountDb Account, Action<string> callback)
    {
        var cert = new CertificateUpdateDto();
        cert.ParentCertThumbprint = Account.Thumbprint;
        return await GenerateCertificate(Account.Email, pinNew).ContinueWith(certificate =>
        {
            var publicCertHelper = _certHelper.ConvertToPublicCertificate(certificate.Result.Item1);

            try
            {
                cert.ParentCertHashSignature = _certHelper.Sign(Account.Guid, pinOld, publicCertHelper.BinaryData).Result;
            }
            catch (Exception e)
            {
                callback.Invoke("Invalid old pin");

                return null;
            }
            cert.PublicCert = publicCertHelper.BinaryData;
            return new Tuple<CertificateUpdateDto, X509Certificate2, string, byte[]>(cert, certificate.Result.Item1, certificate.Result.Item2, certificate.Result.Item3);
        });
    }

    private async Task<Tuple<CertificateUpdateDto, X509Certificate2, string, byte[]>> GenerateCertFromOldCertificateFingerPrint(AccountDb accountDb, Action<string> callback)
    {
        var cert = new CertificateUpdateDto();
        cert.ParentCertThumbprint = accountDb.Thumbprint;
        return await GenerateCertificate(accountDb.Email, null).ContinueWith(certificate =>
        {
            var publicCertHelper = _certHelper.ConvertToPublicCertificate(certificate.Result.Item1);

            try
            {
                var fingerPrintCertificate = _secureRepository.GetCertificateWithFingerPrint(accountDb);
                cert.ParentCertHashSignature = _certHelper.SignByFingerPrint(publicCertHelper.BinaryData, fingerPrintCertificate).Result;
            }
            catch (Exception e)
            {
                callback.Invoke("Invalid old pin");

                return null;
            }
            cert.PublicCert = publicCertHelper.BinaryData;
            return new Tuple<CertificateUpdateDto, X509Certificate2, string, byte[]>(cert, certificate.Result.Item1, certificate.Result.Item2, certificate.Result.Item3);
        });
    }
}

public interface ICertificatesService
{
    Task<Tuple<X509Certificate2, string, byte[]>> GenerateCertificate(string subject, MySecureString pin);

    void CreateFingerPrintCertificate(AccountDb accountDb, MySecureString pin1, Action<string> errorCallback);

    void UpdateCertificate(MySecureString pinNew, MySecureString pinOld, AccountDb accountDb, Action<string> errorAction);

    void UpdateCertificateFingerprint(AccountDb accountDb, Action<string> action);
}