using Newtonsoft.Json;
using passi_maui.utils;

namespace passi_maui.Menu
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditProviderView : ContentPage
    {
        public ProviderDb Provider { get; set; }

        public EditProviderView(ProviderDb provider)
        {
            Provider = JsonConvert.DeserializeObject<ProviderDb>(JsonConvert.SerializeObject(provider));
            InitializeComponent();
            BindingContext = this;

        }

        private void SaveButton_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;
            SecureRepository.UpdateProvider(Provider);
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