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
    public static FinishConfirmationViewModel EnterCorrectCode(RegistrationConfirmationViewModel registrationConfirmationViewModel)
    {
        //TestRestService.Result[ConfigSettings.SignupCheck] = TestBase.SuccesfullResponce();
        registrationConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        registrationConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        registrationConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        registrationConfirmationViewModel.NumbersPad_OnNumberClicked("2");
        registrationConfirmationViewModel.NumbersPad_OnNumberClicked("del");
        registrationConfirmationViewModel.NumbersPad_OnNumberClicked("del");
        registrationConfirmationViewModel.NumbersPad_OnNumberClicked("del");
        registrationConfirmationViewModel.NumbersPad_OnNumberClicked("del");
        var rest = CommonApp.Services.GetService<IRestService>();
        var provider = CommonApp.Services.GetService<ISecureRepository>().LoadProviders().Result;
        var code = rest.ExecutePostAsync(provider.First(x => x.IsDefault), ConfigSettings.Code,
            new SignupDto { Email = registrationConfirmationViewModel.Email, DeviceId = Guid.NewGuid().ToString(), UserGuid = registrationConfirmationViewModel.Account.Guid }).Result;
        //TestRestService.Result[ConfigSettings.SignupCheck] = TestBase.BadResponce("error");
        var strs = code.Content.Replace("\"", "");
        foreach (var str in strs)
        {
            registrationConfirmationViewModel.NumbersPad_OnNumberClicked(str.ToString());
        }
        Assert.IsTrue(TestBase.CurrentView is LoadingViewModel);

        while (!(TestBase.CurrentView is FinishConfirmationViewModel))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is FinishConfirmationViewModel);

        var finishConfirmation = TestBase.CurrentView as FinishConfirmationViewModel;
        return finishConfirmation;
    }

    public static MainView CancelClick(RegistrationConfirmationViewModel registrationConfirmation)
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

    public static RegistrationConfirmationViewModel EnterIncorrectCode(RegistrationConfirmationViewModel registrationConfirmation)
    {
        //TestRestService.Result[ConfigSettings.SignupCheck] = TestBase.BadResponce("error");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");

        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        registrationConfirmation.NumbersPad_OnNumberClicked("1");
        Assert.IsTrue(TestBase.CurrentView is LoadingViewModel);

        while (!(TestBase.CurrentView is RegistrationConfirmationViewModel) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is RegistrationConfirmationViewModel);

        var finishConfirmation = TestBase.CurrentView as RegistrationConfirmationViewModel;
        while (finishConfirmation.ResponseError == "")
        {
            Thread.Sleep(1);
        }

        return finishConfirmation;
    }
}