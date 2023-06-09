using System;
using System.Linq;
using System.Net;
using System.Threading;
using AndroidTests.TestClasses;
using AndroidTests.Tools;
using AppConfig;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using passi_android;
using passi_android.Main;
using passi_android.Registration;
using passi_android.Tools;
using passi_android.utils.Services;
using RestSharp;
using Xamarin.Forms;
using Color = WebApiDto.Auth.Color;

namespace AndroidTests.FunctionalTests
{
    public class UpdateCertificateTests : TestBase
    {

        [Test,Timeout(10000)]
        public void UpdateCertificateWithPin()
        {
            var mainView = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(mainView);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            RegistrationConfirmationTestClass.EnterInCorrectCode(registrationConfirmation);
            Assert.AreEqual(registrationConfirmation.ResponseError, "error");
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            TestRestService.Result[ConfigSettings.SignupConfirmation] = SuccesfullResponce();
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (!mainView._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            var accountView = MainTestClass.OpenAccount(mainPage, mainPage.Accounts[0]);
            accountView.UpdateCertificate_OnClicked(new Button(),null);
            while (!CurrentView.Appeared || !(CurrentView is UpdateCertificateView))
            {
                Thread.Sleep(1);
            }
            var updateCertificateView = CurrentView as UpdateCertificateView;
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.ClearPinOld_OnClicked(new Button(), null);
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("2");
            updateCertificateView.NumbersPad_OnNumberClicked("del");//test editing
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("confirm");
            Assert.AreEqual(4, updateCertificateView.PinOld.Length);
            Assert.AreEqual("****", updateCertificateView.PinOldMasked);

            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.ClearPin1_OnClicked(new Button(), null);
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("2");
            updateCertificateView.NumbersPad_OnNumberClicked("del");//test editing
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("confirm");
            Assert.AreEqual(4, updateCertificateView.Pin1.Length);
            Assert.AreEqual("****", updateCertificateView.Pin1Masked);

            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.ClearPin2_OnClicked(new Button(), null);
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("2");
            updateCertificateView.NumbersPad_OnNumberClicked("del");
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("1");

            TestRestService.Result[ConfigSettings.UpdateCertificate] = TestBase.SuccesfullResponce();

            updateCertificateView.NumbersPad_OnNumberClicked("confirm");
            Assert.AreEqual(4, updateCertificateView.Pin2.Length);
            Assert.AreEqual("****", updateCertificateView.Pin2Masked);

            Assert.IsTrue(CurrentView is LoadingView);
            while (!(CurrentView is MainView) || !CurrentView.Appeared)
            {
                Thread.Sleep(1);
            }
            Assert.IsTrue(CurrentView is MainView);
        }
    }
}