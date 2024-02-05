using MauiApp2.StorageModels;
using Newtonsoft.Json;

namespace MauiApp2.Menu
{
    public partial class EditProviderView : BaseContentPage
    {
        public ProviderDb Provider { get; set; }

        public EditProviderView(ProviderDb provider)
        {
            Provider = JsonConvert.DeserializeObject<ProviderDb>(JsonConvert.SerializeObject(provider));
            if (!App.IsTest)
                InitializeComponent();
            BindingContext = this;
        }

        private void SaveButton_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;
            _secureRepository.UpdateProvider(Provider);
            //save
            button.IsEnabled = true;
            _navigationService.PopModal();
        }
    }
}