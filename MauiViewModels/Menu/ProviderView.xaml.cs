using System;
using MauiViewModels.StorageModels;

namespace MauiViewModels.Menu
{
    public class ProviderView : BaseViewModel
    {
        public ProviderDb Provider { get; set; }

        public ProviderView(ProviderDb provider)
        {
            Provider = provider;
        }

        private void EditButton_OnClicked(object sender, EventArgs e)
        {
            _navigationService.PushModalSinglePage(new EditProviderView(Provider));
        }
    }
}