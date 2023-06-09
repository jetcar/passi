using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
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
using passi_android.Tools;
using WebApiDto;
using WebApiDto.Certificate;
using Xamarin.Forms.Xaml;

namespace passi_android.utils.Services.Certificate
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

        public void SignRequestAndSendResponce(AccountDb _accountDb, MySecureString Pin1, Action<string> callback)
        {

            _navigationService.PushModalSinglePage(new LoadingView(() =>
            {
                _secureRepository.AddfingerPrintKey(_accountDb, Pin1).ContinueWith((result) =>
                {
                    if (result.IsFaulted)
                    {
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            _navigationService.PopModal().ContinueWith((task =>
                            {
                                callback.Invoke("Invalid Pin");

                            }));
                        });

                        return;
                    }

                    _mainThreadService.BeginInvokeOnMainThread(() =>
                    {
                        App.FirstPage.DisplayAlert("Fingerprint", "Fingerprint key added", "Ok");
                        _navigationService.NavigateTop();

                    });
                });
            }));

        }

        public void StartCertGeneration(MySecureString pinNew, MySecureString pinOld, AccountDb Account, Action<string> errorAction)
        {
            _navigationService.PushModalSinglePage(new LoadingView(() =>
            {
                GenerateCert(pinNew, pinOld, Account, errorAction).ContinueWith(certDto =>
                {
                    if (certDto?.Result != null)
                    {
                        var provider = _secureRepository.GetProvider(Account.ProviderGuid);
                        _restService.ExecutePostAsync<Tuple<CertificateUpdateDto, X509Certificate2,string, byte[]>>(provider, provider.UpdateCertificate, certDto.Result).ContinueWith((response) =>
                        {
                            if (response.Result.IsSuccessful)
                            {
                                var certificateBase64 = Convert.ToBase64String(certDto?.Result.Item4); //importable certificate

                                var publicCertHelper = _certHelper.ConvertToPublicCertificate(certDto.Result.Item2);

                                Account.Salt = certDto.Result.Item3;
                                Account.PrivateCertBinary = certificateBase64;
                                Account.pinLength = pinNew?.Length ?? 0;
                                Account.Thumbprint = certDto.Result.Item2.Thumbprint;
                                Account.ValidFrom = certDto.Result.Item2.NotBefore;
                                Account.ValidTo = certDto.Result.Item2.NotAfter;
                                Account.PublicCertBinary = publicCertHelper.BinaryData;

                                _secureRepository.UpdateAccount(Account);
                                _secureRepository.AddfingerPrintKey(Account, pinNew).GetAwaiter().GetResult();

                                _mainThreadService.BeginInvokeOnMainThread(() => { _navigationService.NavigateTop(); });
                            }
                            if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
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

        public async Task<Tuple<CertificateUpdateDto, X509Certificate2, string, byte[]>> GenerateCert(MySecureString pinNew, MySecureString pinOld, AccountDb Account, Action<string> callback)
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
    }
    public interface ICertificatesService
    {
        Task<Tuple<X509Certificate2, string, byte[]>> GenerateCertificate(string subject, MySecureString pin);
        void SignRequestAndSendResponce(AccountDb _accountDb, MySecureString Pin1, Action<string> callback);
        void StartCertGeneration(MySecureString pinNew, MySecureString pinOld, AccountDb Account, Action<string> errorAction);
    }
}