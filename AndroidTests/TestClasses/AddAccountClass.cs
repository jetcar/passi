using System.Threading;
using AndroidTests.Tools;
using NUnit.Framework;
using passi_android;
using passi_android.Registration;
using passi_android.Tools;
using Xamarin.Forms;

namespace AndroidTests.TestClasses;

public class AddAccountClass
{
    public static RegistrationConfirmationView ClickConfirm(AddAccountView addAccountView)
    {
        addAccountView.Button_OnClicked(new Button(), null);

        Assert.IsTrue(TestBase.CurrentPage is LoadingView);

        while (TestBase.CurrentPage is LoadingView)
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentPage is RegistrationConfirmationView);

        var registrationConfirmation = TestBase.CurrentPage as RegistrationConfirmationView;
        return registrationConfirmation;
    }
}