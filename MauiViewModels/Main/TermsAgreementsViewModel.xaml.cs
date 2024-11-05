namespace MauiViewModels.Main
{
    public class TermsAgreementsViewModel : PassiBaseViewModel
    {
        public TermsAgreementsViewModel()
        {
        }

        public void Button_OnAgreeClicked()
        {
            _navigationService.PushModalSinglePage(new AddAccountViewModel());
        }

        public void Button_OnCancelClicked()
        {
            _navigationService.NavigateTop();
        }
    }
}