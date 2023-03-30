using passi_maui.utils;

namespace passi_maui
{
    public partial class TermsAgreements : ContentPage
    {
        public TermsAgreements()
        {
            InitializeComponent();
        }

        private void Button_OnAgreeClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            Navigation.PushModalSinglePage(new AddAccountPage());
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