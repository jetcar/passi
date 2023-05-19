using System;
using System.Linq;
using passi_android.utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Menu
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddProviderView : ContentPage
    {
        public ProviderDb Provider { get; set; }
        private ISecureRepository _secureRepository;
        private INavigationService Navigation;
        public AddProviderView()
        {
            _secureRepository = App.Services.GetService<ISecureRepository>();

            Navigation = App.Services.GetService<INavigationService>();
            Provider = new ProviderDb();
            var defaultProfider = _secureRepository.LoadProviders().First(x => x.IsDefault);
            _secureRepository.CopyAll(defaultProfider,Provider);
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
            _secureRepository.AddProvider(Provider);

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