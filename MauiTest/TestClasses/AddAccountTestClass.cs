using System.Threading;
using MauiTest.Tools;
using MauiViewModels.Main;
using MauiViewModels.Registration;
using MauiViewModels.Tools;
using NUnit.Framework;

namespace MauiTest.TestClasses;

public class AddAccountTestClass
{
    public static RegistrationConfirmationViewModel ClickConfirm(AddAccountViewModel addAccountView)
    {
        addAccountView.Button_OnClicked();

        Assert.IsTrue(TestBase.CurrentView is LoadingViewModel);

        while (!(TestBase.CurrentView is RegistrationConfirmationViewModel))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is RegistrationConfirmationViewModel);

        var registrationConfirmation = TestBase.CurrentView as RegistrationConfirmationViewModel;
        return registrationConfirmation;
    }
}