using MauiViewModels.StorageModels;
using MauiViewModels.utils.Services;

namespace MauiViewModels.Menu
{
    public class ProviderViewModel : BaseViewModel
    {
        public ProviderDb Provider { get; set; }

        public ProviderViewModel(ProviderDb provider)
        {
            Provider = provider;
        }

        public void EditButton_OnClicked()
        {
            _navigationService.PushModalSinglePage(new EditProviderViewModel(Provider));
        }

        public void BackButton_OnClicked()
        {
            _navigationService.PopModal();
        }
    }
}