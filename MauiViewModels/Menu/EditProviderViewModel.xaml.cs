using MauiViewModels.StorageModels;
using Newtonsoft.Json;

namespace MauiViewModels.Menu
{
    public class EditProviderViewModel : BaseViewModel
    {
        public ProviderDb Provider { get; set; }

        public EditProviderViewModel(ProviderDb provider)
        {
            Provider = JsonConvert.DeserializeObject<ProviderDb>(JsonConvert.SerializeObject(provider));
        }

        public void SaveButton_OnClicked()
        {
            _secureRepository.UpdateProvider(Provider);
            //save
            _navigationService.PopModal();
        }
    }
}