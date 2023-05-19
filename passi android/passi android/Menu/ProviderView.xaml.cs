using System;
using passi_android.utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Menu
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProviderView : ContentPage
    {
        public ProviderDb Provider { get; set; }

        private INavigationService Navigation;
        public ProviderView(ProviderDb provider)
        {
            Navigation = App.Services.GetService<INavigationService>();
            Provider = provider;
            InitializeComponent();
            BindingContext = this;

        }

        private void EditButton_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            Navigation.PushModalSinglePage(new EditProviderView(Provider));
            button.IsEnabled = true;
        }


        private void Button_Back(object sender, EventArgs e)
        {
            Navigation.PopModal();
        }
    }
}