using System;
using MauiViewModels.StorageModels;
using Newtonsoft.Json;

namespace MauiViewModels.Menu
{
    public class EditProviderView : BaseViewModel
    {
        public ProviderDb Provider { get; set; }

        public EditProviderView(ProviderDb provider)
        {
            Provider = JsonConvert.DeserializeObject<ProviderDb>(JsonConvert.SerializeObject(provider));
        }

        private void SaveButton_OnClicked(object sender, EventArgs e)
        {
            _secureRepository.UpdateProvider(Provider);
            //save
            _navigationService.PopModal();
        }
    }
}