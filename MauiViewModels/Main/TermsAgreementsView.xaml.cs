using System;

namespace MauiViewModels.Main
{
    public class TermsAgreementsView : BaseViewModel
    {
        public TermsAgreementsView()
        {
        }

        public void Button_OnAgreeClicked()
        {
            _navigationService.PushModalSinglePage(new AddAccountView());
        }

        public void Button_OnCancelClicked()
        {
            _navigationService.NavigateTop();
        }
    }
}