using System;
using Newtonsoft.Json;
using passi_android.StorageModels;
using passi_android.utils.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Menu
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditProviderView : ContentPage
    {
        public ProviderDb Provider { get; set; }
        private ISecureRepository _secureRepository;
        private INavigationService _navigationService;
        public EditProviderView(ProviderDb provider)
        {
            _secureRepository = App.Services.GetService<ISecureRepository>();
            _navigationService = App.Services.GetService<INavigationService>();
            Provider = JsonConvert.DeserializeObject<ProviderDb>(JsonConvert.SerializeObject(provider));
            if(!App.IsTest)
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
        private void Button_Back(object sender, EventArgs e)
        {
            _navigationService.PopModal();
        }


    }
}