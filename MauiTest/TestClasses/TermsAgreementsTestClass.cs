using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.Main;
using NUnit.Framework;

namespace MauiTest.TestClasses;

public class TermsAgreementsTestClass
{
    public static AddAccountView ClickAgree(TermsAgreementsView tcView)
    {
        tcView.Button_OnAgreeClicked();

        Assert.IsTrue(TestBase.CurrentView is AddAccountView);

        var addAccountView = TestBase.CurrentView as AddAccountView;
        return addAccountView;
    }

    public static MainView ClickCancel(TermsAgreementsView tcView)
    {
        TestNavigationService.navigationsCount = 0;
        tcView.Button_OnCancelClicked();

        Assert.IsTrue(TestBase.CurrentView is MainView);

        var mainView = TestBase.CurrentView as MainView;
        Assert.AreEqual(1, TestNavigationService.navigationsCount);
        return mainView;
    }
}