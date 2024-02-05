using passi_android.StorageModels;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Menu
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProviderView : BaseContentPage
    {
        public ProviderDb Provider { get; set; }

        public ProviderView(ProviderDb provider)
        {
            Provider = provider;
            if (!App.IsTest)
                InitializeComponent();
            BindingContext = this;
        }

        private void EditButton_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            _navigationService.PushModalSinglePage(new EditProviderView(Provider));
            button.IsEnabled = true;
        }
    }
}