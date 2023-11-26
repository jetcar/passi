using System.Net;
using System.Security.Cryptography.X509Certificates;
using MauiApp2.StorageModels;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using MauiApp2.Tools;
using WebApiDto;
using WebApiDto.Certificate;

namespace MauiApp2.utils.Services.Certificate
{
    public class CertificatesService : ICertificatesService
    {
        INavigationService _navigationService;
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
            var random = new SecureRandom();
            var certificateGenerator = new X509V3CertificateGenerator();

            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            var sha512 = subject;
            certificateGenerator.SetIssuerDN(new X509Name($"C=NL, O=Passi, CN={sha512}"));
            certificateGenerator.SetSubjectDN(new X509Name($"C=NL, O=Passi, CN={sha512}"));
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(1));

            const int strength = 2048;
            var keyGenerationParameters = new KeyGenerationParameters(random, strength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);

            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            var issuerKeyPair = subjectKeyPair;
            const string signatureAlgorithm = "SHA256WithRSA";
            var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, issuerKeyPair.Private);
            var bouncyCert = certificateGenerator.Generate(signatureFactory);

            // Lets convert it to X509Certificate2
            X509Certificate2 certificate;

            Pkcs12Store store = new Pkcs12StoreBuilder().Build();
            store.SetKeyEntry($"{sha512}_key", new AsymmetricKeyEntry(subjectKeyPair.Private), new[] { new X509CertificateEntry(bouncyCert) });
            MySecureString password = new MySecureString(Guid.NewGuid().ToString());
            MySecureString fullPassword = new MySecureString(password);
            if (pin != null)
                fullPassword.Append(pin);

            using (var ms = new MemoryStream())
            {
                store.Save(ms, fullPassword.SecureStringToString().ToCharArray(), random);
                var rawBytes = ms.ToArray();
                certificate = new X509Certificate2(rawBytes, fullPassword.SecureStringToString(), X509KeyStorageFlags.Exportable);
                var result = new Tuple<X509Certificate2, string, byte[]>(certificate, password.SecureStringToString(), rawBytes);
                return result;
            }
        }

        public void CreateFingerPrintCertificate(AccountDb accountDb, MySecureString pin1, Action<string> errorCallback)
        {
            _navigationService.PushModalSinglePage(new LoadingView(() =>
            {
                try
                {
                    var cert = _secureRepository.GetCertificateWithKey(pin1, accountDb);
                    _secureRepository.SaveFingerPrintKey(accountDb, cert).ContinueWith((result) =>
                    {
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            App.FirstPage.DisplayAlert("Fingerprint", "Fingerprint key added", "Ok");
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
            _navigationService.PushModalSinglePage(new LoadingView(() =>
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
                                _navigationService.PopModal().ContinueWith((task) =>
                                {
                                    errorAction.Invoke(JsonConvert.DeserializeObject<ApiResponseDto<string>>(response.Result.Content).Message);
                                });
                            }
                            else
                            {
                                _navigationService.PopModal().ContinueWith((task) =>
                                {
                                    errorAction.Invoke("Network error. Try again");
                                });
                            }
                        });
                    }
                });
            }));
        }
        public void UpdateCertificateFingerprint(AccountDb accountDb, Action<string> errorAction)
        {
            _navigationService.PushModalSinglePage(new LoadingView(() =>
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
                                _navigationService.PopModal().ContinueWith((task) =>
                                {
                                    errorAction.Invoke(JsonConvert.DeserializeObject<ApiResponseDto<string>>(response.Result.Content).Message);
                                });
                            }
                            else
                            {
                                _navigationService.PopModal().ContinueWith((task) =>
                                {
                                    errorAction.Invoke("Network error. Try again");
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
}