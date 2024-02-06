using System.Threading;
using MauiTest.Tools;
using MauiViewModels.Main;
using MauiViewModels.Registration;
using MauiViewModels.Tools;
using NUnit.Framework;

namespace MauiTest.TestClasses;

public class AddAccountTestClass
{
    public static RegistrationConfirmationView ClickConfirm(AddAccountView addAccountView)
    {
        addAccountView.Button_OnClicked();

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