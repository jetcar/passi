using System;
using Newtonsoft.Json;
using passi_android.utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Menu
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditProviderView : ContentPage
    {
        public ProviderDb Provider { get; set; }
        private ISecureRepository _secureRepository;
        private INavigationService Navigation;
        public EditProviderView(ProviderDb provider)
        {
            _secureRepository = App.Services.GetService<ISecureRepository>();
            Navigation = App.Services.GetService<INavigationService>();
            Provider = JsonConvert.DeserializeObject<ProviderDb>(JsonConvert.SerializeObject(provider));
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
            Navigation.PopModal();
        }
        private void Button_Back(object sender, EventArgs e)
        {
            Navigation.PopModal();
        }


    }
}