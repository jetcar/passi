using AndroidTests.Tools;
using NUnit.Framework;
using passi_android.Main;
using passi_android.Registration;
using passi_android.Tools;
using System.Threading;
using Xamarin.Forms;

namespace AndroidTests.TestClasses;

public class AddAccountTestClass
{
    public static RegistrationConfirmationView ClickConfirm(AddAccountView addAccountView)
    {
        addAccountView.Button_OnClicked(new Button(), null);

        Assert.IsTrue(TestBase.CurrentView is LoadingView);

        while (!(TestBase.CurrentView is RegistrationConfirmationView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is RegistrationConfirmationView);

        var registrationConfirmation = TestBase.CurrentView as RegistrationConfirmationView;
        return registrationConfirmation;
    }
}