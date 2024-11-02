using System;
using System.Linq;
using MauiViewModels.StorageModels;

namespace MauiViewModels.Menu
{
    public class AddProviderViewModel : BaseViewModel
    {
        public ProviderDb Provider { get; set; }

        public AddProviderViewModel()
        {
            Provider = new ProviderDb();
            var defaultProfider = _secureRepository.LoadProviders().Result.First(x => x.IsDefault);
            _secureRepository.CopyAll(defaultProfider, Provider);
            Provider.IsDefault = false;
            Provider.Guid = Guid.NewGuid();
            Provider.Name = "";
            Provider.PassiWebApiUrl = "https://";
        }

        public void SaveButton_OnClicked()
        {
            _secureRepository.AddProvider(Provider);

            //save
            _navigationService.PopModal();
        }

        public void CancelButton_OnClicked()
        {
            _navigationService.PopModal();
        }
    }
}