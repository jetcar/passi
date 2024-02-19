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

        Assert.IsTrue(TestBase.CurrentView is AddAccountViewModel);

        var addAccountView = TestBase.CurrentView as AddAccountViewModel;
        return addAccountView;
    }

    public static MainView ClickCancel(TermsAgreementsViewModel tcView)
    {
        TestNavigationService.navigationsCount = 0;
        tcView.Button_OnCancelClicked();

        Assert.IsTrue(TestBase.CurrentView is MainView);

        var mainView = TestBase.CurrentView as MainView;
        Assert.AreEqual(1, TestNavigationService.navigationsCount);
        return mainView;
    }
}