namespace MauiApp2.Main
{
    public partial class TermsAgreementsView : BaseContentPage
    {
        public TermsAgreementsView()
        {
            if (!App.IsTest)
                InitializeComponent();
        }

        public void Button_OnAgreeClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            _navigationService.PushModalSinglePage(new AddAccountView());
            element.IsEnabled = true;
        }

        public void Button_OnCancelClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            _navigationService.NavigateTop();
            element.IsEnabled = true;
        }
    }
}