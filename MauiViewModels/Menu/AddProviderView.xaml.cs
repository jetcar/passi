using System;
using System.Linq;
using MauiViewModels.StorageModels;

namespace MauiViewModels.Menu
{
    public class AddProviderView : BaseContentPage
    {
        public ProviderDb Provider { get; set; }

        public AddProviderView()
        {
            Provider = new ProviderDb();
            var defaultProfider = _secureRepository.LoadProviders().Result.First(x => x.IsDefault);
            _secureRepository.CopyAll(defaultProfider, Provider);
            Provider.IsDefault = false;
            Provider.Guid = Guid.NewGuid();
            Provider.Name = "";
            Provider.WebApiUrl = "https://";
        }

        private void SaveButton_OnClicked(object sender, EventArgs e)
        {
            _secureRepository.AddProvider(Provider);

            //save
            _navigationService.PopModal();
        }
    }
}