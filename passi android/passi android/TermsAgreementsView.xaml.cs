using System;
using passi_android.utils.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TermsAgreementsView : ContentPage
    {
        private INavigationService Navigation;
        public TermsAgreementsView()
        {
            Navigation = App.Services.GetService<INavigationService>();
            if(!App.IsTest)
            InitializeComponent();
        }

        public void Button_OnAgreeClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            Navigation.PushModalSinglePage(new AddAccountView());
            element.IsEnabled = true;
        }

        private void Button_OnCancelClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            Navigation.NavigateTop();
            element.IsEnabled = true;
        }
    }
}