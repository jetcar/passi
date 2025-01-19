using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.Main;
using NUnit.Framework;

namespace MauiTest.TestClasses;

public class TermsAgreementsTestClass
{
    public static AddAccountViewModel ClickAgree(TermsAgreementsViewModel tcView)
    {
        tcView.Button_OnAgreeClicked();

        Assert.That(TestBase.CurrentView is AddAccountViewModel);

        var addAccountView = TestBase.CurrentView as AddAccountViewModel;
        return addAccountView;
    }

    public static MainView ClickCancel(TermsAgreementsViewModel tcView)
    {
        TestNavigationService.navigationsCount = 0;
        tcView.Button_OnCancelClicked();

        Assert.That(TestBase.CurrentView is MainView);

        var mainView = TestBase.CurrentView as MainView;
        Assert.That(1, Is.EqualTo(TestNavigationService.navigationsCount));
        return mainView;
    }
}