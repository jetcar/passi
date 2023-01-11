using System;
using passi_android.utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Menu
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditProviderView : ContentPage
    {
        public ProviderDb Provider { get; set; }

        public EditProviderView(ProviderDb provider)
        {
            Provider = provider;
            InitializeComponent();
            BindingContext = this;

        }

        private void SaveButton_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            //save
            button.IsEnabled = true;
        }
        private void Button_Back(object sender, EventArgs e)
        {
            Navigation.PopModal();
        }

     
    }
}