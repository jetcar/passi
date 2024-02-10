using System.Threading;
using AppConfig;
using MauiTest.TestClasses;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.Main;
using MauiViewModels.Tools;
using NUnit.Framework;

namespace MauiTest.FunctionalTests
{
    public class UpdateCertificateTests : TestBase
    {
        [Test, Timeout(10000)]
        public void UpdateCertificateWithPin()
        {
            var mainView = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(mainView);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            RegistrationConfirmationTestClass.EnterIncorrectCode(registrationConfirmation);
            Assert.AreEqual(registrationConfirmation.ResponseError, "Code not found");
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            //TestRestService.Result[ConfigSettings.SignupConfirmation] = SuccesfullResponce();
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (!mainView._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            var accountView = MainTestClass.OpenAccount(mainPage, mainPage.Accounts[0]);
            accountView.UpdateCertificate_OnClicked();
            while (!CurrentView.Appeared || !(CurrentView is UpdateCertificateView))
            {
                Thread.Sleep(1);
            }
            var updateCertificateView = CurrentView as UpdateCertificateView;
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.ClearPinOld_OnClicked();
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
            updateCertificateView.ClearPin1_OnClicked();
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
            updateCertificateView.ClearPin2_OnClicked();
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("2");
            updateCertificateView.NumbersPad_OnNumberClicked("del");
            updateCertificateView.NumbersPad_OnNumberClicked("1");
            updateCertificateView.NumbersPad_OnNumberClicked("1");

            //TestRestService.Result[ConfigSettings.UpdateCertificate] = TestBase.SuccesfullResponce();

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

        [Test, Timeout(20000)]
        public void UpdateCertificateNoPin()
        {
            var mainView = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(mainView);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            RegistrationConfirmationTestClass.EnterIncorrectCode(registrationConfirmation);
            Assert.AreEqual(registrationConfirmation.ResponseError, "Code not found");
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            //TestRestService.Result[ConfigSettings.SignupConfirmation] = SuccesfullResponce();
            var mainPage = FinishConfirmationTestClass.FinishRegistrationSkipPin(finishConfirmation);
            while (!mainView._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            var accountView = MainTestClass.OpenAccount(mainPage, mainPage.Accounts[0]);
            //TestRestService.Result[ConfigSettings.UpdateCertificate] = TestBase.SuccesfullResponce();
            accountView.UpdateCertificate_OnClicked();

            Assert.IsTrue(CurrentView is LoadingView);
            while (!(CurrentView is MainView) || !CurrentView.Appeared)
            {
                Thread.Sleep(1);
            }
            Assert.IsTrue(CurrentView is MainView);
        }

        [Test, Timeout(10000)]
        public void UpdateCertificateNoPinFingerprint()
        {
            var mainView = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(mainView);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            RegistrationConfirmationTestClass.EnterIncorrectCode(registrationConfirmation);
            Assert.AreEqual(registrationConfirmation.ResponseError, "Code not found");
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            //TestRestService.Result[ConfigSettings.SignupConfirmation] = SuccesfullResponce();
            var mainPage = FinishConfirmationTestClass.FinishRegistrationSkipPin(finishConfirmation);
            while (!mainView._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            var accountView = MainTestClass.OpenAccount(mainPage, mainPage.Accounts[0]);

            FingerPrintTestClass.AddFingerPrintNoPin(accountView);

            while (!(CurrentView is MainView))
            {
                Thread.Sleep(1);
            }

            Assert.IsTrue(CurrentView is MainView);

            accountView = MainTestClass.OpenAccount(mainPage, mainPage.Accounts[0]);
            //TestRestService.Result[ConfigSettings.UpdateCertificate] = TestBase.SuccesfullResponce();
            accountView.UpdateCertificate_OnClicked();
            TestBase.TouchFingerPrintWithGoodResult();
            while (!(CurrentView is LoadingView) || !CurrentView.Appeared)
            {
                Thread.Sleep(1);
            }
            Assert.IsTrue(CurrentView is LoadingView);
            while (!(CurrentView is MainView) || !CurrentView.Appeared)
            {
                Thread.Sleep(1);
            }
            Assert.IsTrue(CurrentView is MainView);
        }
    }
}