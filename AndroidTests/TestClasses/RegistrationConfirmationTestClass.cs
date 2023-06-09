using System.Threading;
using AndroidTests.Tools;
using AppConfig;
using NUnit.Framework;
using passi_android;
using passi_android.Main;
using passi_android.Registration;
using passi_android.Tools;
using Xamarin.Forms;

namespace AndroidTests.TestClasses;

public class RegistrationConfirmationTestClass
{
    public static FinishConfirmationView EnterCorrectCode(RegistrationConfirmationView registrationConfirmationView)
    {

        TestRestService.Result[ConfigSettings.SignupCheck] = TestBase.SuccesfullResponce();
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("2");
        registrationConfirmationView.NumbersPad_OnNumberClicked("del");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        Assert.IsTrue(TestBase.CurrentView is LoadingView);

        while (!(TestBase.CurrentView is passi_android.Registration.FinishConfirmationView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is passi_android.Registration.FinishConfirmationView);

        var finishConfirmation = TestBase.CurrentView as FinishConfirmationView;
        return finishConfirmation;
    }

    public static MainView CancelClick(RegistrationConfirmationView registrationConfirmation)
    {
        registrationConfirmation.CancelButton_OnClicked(new Button(), null);

        while (!(TestBase.CurrentView is MainView) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is MainView);

        var finishConfirmation = TestBase.CurrentView as MainView;
        return finishConfirmation;

    }

    public static RegistrationConfirmationView EnterInCorrectCode(RegistrationConfirmationView registrationConfirmation)
    {
        TestRestService.Result[ConfigSettings.SignupCheck] = TestBase.BadResponce("error");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");

        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        Assert.IsTrue(TestBase.CurrentView is LoadingView);

        while (!(TestBase.CurrentView is passi_android.Registration.RegistrationConfirmationView) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is passi_android.Registration.RegistrationConfirmationView);

        var finishConfirmation = TestBase.CurrentView as RegistrationConfirmationView;
        while (finishConfirmation.ResponseError == "")
        {
            Thread.Sleep(1);
        }

        return finishConfirmation;
    }
}