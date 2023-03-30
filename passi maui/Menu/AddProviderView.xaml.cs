using passi_maui.utils;

namespace passi_maui.Menu
{
    public partial class AddProviderView : ContentPage
    {
        public ProviderDb Provider { get; set; }

        public AddProviderView()
        {
            Provider = new ProviderDb();
            var defaultProfider = SecureRepository.LoadProviders().First(x => x.IsDefault);
            SecureRepository.CopyAll(defaultProfider,Provider);
            Provider.IsDefault = false;
            Provider.Guid = Guid.NewGuid();
            Provider.Name = "";
            Provider.WebApiUrl = "https://";
            InitializeComponent();
            BindingContext = this;

        }

        private void SaveButton_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;
            SecureRepository.AddProvider(Provider);

            //save
            button.IsEnabled = true;
            Navigation.PopModal();

        }
        private void Button_Back(object sender, EventArgs e)
        {
            Navigation.PopModal();
        }

     
    }
}