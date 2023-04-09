using Newtonsoft.Json;
using passi_maui.utils;

namespace passi_maui.Menu
{
    [QueryProperty("Provider", "Provider")]
    public partial class EditProviderView : ContentPage
    {
        private ProviderDb _provider;

        public ProviderDb Provider
        {
            get => _provider;
            set
            {
                if (Equals(value, _provider)) return;
                _provider = value;
                OnPropertyChanged();
            }
        }

        public EditProviderView()
        {
            InitializeComponent();
            BindingContext = this;

        }

        protected override void OnAppearing()
        {
            Provider = JsonConvert.DeserializeObject<ProviderDb>(JsonConvert.SerializeObject(Provider));

            base.OnAppearing();
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