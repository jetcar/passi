using System;
using System.Linq;
using System.Threading;
using AppConfig;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.Registration;
using MauiViewModels.Tools;
using MauiViewModels.utils.Services;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using WebApiDto.SignUp;

namespace MauiTest.TestClasses;

public class RegistrationConfirmationTestClass
{
    public static FinishConfirmationView EnterCorrectCode(RegistrationConfirmationView registrationConfirmationView)
    {
        //TestRestService.Result[ConfigSettings.SignupCheck] = TestBase.SuccesfullResponce();
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("1");
        registrationConfirmationView.NumbersPad_OnNumberClicked("2");
        registrationConfirmationView.NumbersPad_OnNumberClicked("del");
        registrationConfirmationView.NumbersPad_OnNumberClicked("del");
        registrationConfirmationView.NumbersPad_OnNumberClicked("del");
        registrationConfirmationView.NumbersPad_OnNumberClicked("del");
        var rest = App.Services.GetService<IRestService>();
        var provider = App.Services.GetService<ISecureRepository>().LoadProviders().Result;
        var code = rest.ExecutePostAsync(provider.First(x => x.IsDefault), ConfigSettings.Code,
            new SignupDto { Email = registrationConfirmationView.Email, DeviceId = Guid.NewGuid().ToString(), UserGuid = registrationConfirmationView.Account.Guid }).Result;
        //TestRestService.Result[ConfigSettings.SignupCheck] = TestBase.BadResponce("error");
        var strs = code.Content.Replace("\"", "");
        foreach (var str in strs)
        {
            registrationConfirmationView.NumbersPad_OnNumberClicked(str.ToString());
        }
        Assert.IsTrue(TestBase.CurrentView is LoadingView);

        while (!(TestBase.CurrentView is FinishConfirmationView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is FinishConfirmationView);

        var finishConfirmation = TestBase.CurrentView as FinishConfirmationView;
        return finishConfirmation;
    }

    public static MainView CancelClick(RegistrationConfirmationView registrationConfirmation)
    {
        TestNavigationService.navigationsCount = 0;
        registrationConfirmation.CancelButton_OnClicked();

        while (!(TestBase.CurrentView is MainView) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is MainView);

        var finishConfirmation = TestBase.CurrentView as MainView;
        Assert.AreEqual(1, TestNavigationService.navigationsCount);
        return finishConfirmation;
    }

    public static RegistrationConfirmationView EnterIncorrectCode(RegistrationConfirmationView registrationConfirmation)
    {
        //TestRestService.Result[ConfigSettings.SignupCheck] = TestBase.BadResponce("error");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");

        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        Assert.IsTrue(TestBase.CurrentView is LoadingView);

        while (!(TestBase.CurrentView is RegistrationConfirmationView) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is RegistrationConfirmationView);

        var finishConfirmation = TestBase.CurrentView as RegistrationConfirmationView;
        while (finishConfirmation.ResponseError == "")
        {
            Thread.Sleep(1);
        }

        return finishConfirmation;
    }
}