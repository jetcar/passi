using System;

namespace MauiViewModels.Main
{
    public class TermsAgreementsViewModel : BaseViewModel
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