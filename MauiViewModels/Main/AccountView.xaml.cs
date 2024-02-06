using System;
using MauiViewModels.FingerPrint;
using MauiViewModels.StorageModels;

namespace MauiViewModels.Main
{
    public class AccountView : BaseContentPage
    {
        public AccountDb AccountDb { get; set; }
        private string _email;
        private string _thumbprint;
        private string _validFrom;
        private string _validTo;
        private string _providerName;

        private string _message;

        public AccountView(AccountDb accountDb)
        {
            AccountDb = accountDb;

            try
            {
                Email = AccountDb.Email;
                Thumbprint = AccountDb.Thumbprint;
                ValidFrom = AccountDb.ValidFrom.ToShortDateString();
                ValidTo = AccountDb.ValidTo.ToShortDateString();
                Email = accountDb.Email;
                Thumbprint = AccountDb.Thumbprint;
                ProviderName = AccountDb.Provider?.Name;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public string ProviderName
        {
            get => _providerName;
            set
            {
                if (value == _providerName) return;
                _providerName = value;
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

        public string Thumbprint
        {
            get => _thumbprint;
            set
            {
                _thumbprint = value;
                OnPropertyChanged();
            }
        }

        public string ValidFrom
        {
            get => _validFrom;
            set
            {
                _validFrom = value;
                OnPropertyChanged();
            }
        }

        public string ValidTo
        {
            get => _validTo;
            set
            {
                _validTo = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public void UpdateCertificate_OnClicked()
        {
            if (AccountDb.pinLength > 0)
            {
                _navigationService.PushModalSinglePage(new UpdateCertificateView() { Account = AccountDb });
            }
            else if (AccountDb.HaveFingerprint)
            {
                App.FingerPrintReadingResult = (result) =>
                {
                    if (result.ErrorMessage == null)
                    {
                        _certificatesService.UpdateCertificateFingerprint(AccountDb, (error) =>
                        {
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PopModal().ContinueWith((task =>
                                {
                                    Message = result.ErrorMessage;
                                    App.StartFingerPrintReading();
                                }));
                            });
                        });
                    }
                    else
                    {
                        Message = result.ErrorMessage;
                        App.StartFingerPrintReading();
                    }
                };

                App.StartFingerPrintReading();
            }
            else
            {
                _certificatesService.UpdateCertificate(null, null, AccountDb, (error) =>
                {
                    Message = error;
                });
            }
        }

        public void AddBiometric_Button_OnClicked()
        {
            App.FingerPrintReadingResult = (result) =>
            {
                if (result.ErrorMessage == null)
                {
                    if (AccountDb.pinLength > 0)
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            _navigationService.PushModalSinglePage(new FingerPrintConfirmByPinView(AccountDb));
                        });
                    else
                    {
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            _certificatesService.CreateFingerPrintCertificate(AccountDb, null,
                                (error) =>
                                {
                                    Message = error;
                                });
                        });
                    }
                }
                else
                {
                    Message = result.ErrorMessage;
                    App.StartFingerPrintReading();
                }
            };

            App.StartFingerPrintReading();
        }

        public override void OnDisappearing()
        {
            App.FingerPrintReadingResult = null;
            base.OnDisappearing();
        }
    }
}