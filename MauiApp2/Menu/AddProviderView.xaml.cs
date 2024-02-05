using MauiApp2.StorageModels;

namespace MauiApp2.Menu
{
    public partial class AddProviderView : BaseContentPage
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
            if (!App.IsTest)
                InitializeComponent();
            BindingContext = this;
        }

        private void SaveButton_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;
            _secureRepository.AddProvider(Provider);

            //save
            button.IsEnabled = true;
            _navigationService.PopModal();
        }
    }
}