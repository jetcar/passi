using MauiViewModels.Main;

namespace MauiApp2.Main
{
    public partial class TermsAgreementsView : BaseContentPage
    {
        public TermsAgreementsView()
        {
            InitializeComponent();
        }

        public void Button_OnAgreeClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            ((TermsAgreementsViewModel)BindingContext).Button_OnAgreeClicked();
            element.IsEnabled = true;
        }

        public void Button_OnCancelClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            ((TermsAgreementsViewModel)BindingContext).Button_OnCancelClicked();
            element.IsEnabled = true;
        }
    }
}