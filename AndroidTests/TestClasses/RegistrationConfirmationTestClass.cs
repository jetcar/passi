using System.Threading;
using AndroidTests.Tools;
using AppConfig;
using NUnit.Framework;
using passi_android.Registration;
using passi_android.Tools;

namespace AndroidTests.TestClasses;

public class RegistrationConfirmationTestClass
{
    public static FinishConfirmationView EnterCorrectCode(RegistrationConfirmationView registrationConfirmationView)
    {

        TestRestService.Result[ConfigSettings.SignupCheck] = TestBase.SuccesfullResponce();
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        Assert.IsTrue(TestBase.CurrentPage is LoadingView);

        while (!(TestBase.CurrentPage is passi_android.Registration.FinishConfirmationView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentPage is passi_android.Registration.FinishConfirmationView);

        var finishConfirmation = TestBase.CurrentPage as FinishConfirmationView;
        return finishConfirmation;
    }
}