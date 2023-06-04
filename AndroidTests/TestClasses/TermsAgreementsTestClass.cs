using AndroidTests.Tools;
using NUnit.Framework;
using passi_android;
using Xamarin.Forms;

namespace AndroidTests.TestClasses;

public class TermsAgreementsTestClass
{
    public static AddAccountView ClickAgree(TermsAgreementsView tcView)
    {
        tcView.Button_OnAgreeClicked(new Button(), null);

        Assert.IsTrue(TestBase.CurrentPage is AddAccountView);

        var addAccountView = TestBase.CurrentPage as AddAccountView;
        return addAccountView;
    }
}