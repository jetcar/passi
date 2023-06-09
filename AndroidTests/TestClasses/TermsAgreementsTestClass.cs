using AndroidTests.Tools;
using NUnit.Framework;
using passi_android;
using passi_android.Main;
using Xamarin.Forms;

namespace AndroidTests.TestClasses;

public class TermsAgreementsTestClass
{
    public static AddAccountView ClickAgree(TermsAgreementsView tcView)
    {
        tcView.Button_OnAgreeClicked(new Button(), null);

        Assert.IsTrue(TestBase.CurrentView is AddAccountView);

        var addAccountView = TestBase.CurrentView as AddAccountView;
        return addAccountView;
    }

    public static MainView ClickCancel(TermsAgreementsView tcView)
    {
        tcView.Button_OnCancelClicked(new Button(), null);

        Assert.IsTrue(TestBase.CurrentView is MainView);

        var mainView = TestBase.CurrentView as MainView;
        return mainView;
    }
}