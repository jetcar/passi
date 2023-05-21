using System.Threading;
using AndroidTests.TestClasses;
using AndroidTests.Tools;
using AppConfig;
using NUnit.Framework;
using passi_android;

namespace AndroidTests.FunctionalTests
{
    public class RegistrationTests : TestBase
    {
        [Test]
        public void AddAccountWithPin()
        {
            var page = MainClass.OpenMainPage();
            var tcView = MainClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationClass.EnterCorrectCode(registrationConfirmation);
            TestRestService.Result[ConfigSettings.SignupConfirmation] = SuccesfullResponce();
            var mainPage = FinishConfirmationClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
        }
        [Test]
        public void AddAccountWithPinAndFingerPrint()
        {
            var page = MainClass.OpenMainPage();
            var tcView = MainClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationClass.EnterCorrectCode(registrationConfirmation);
            TestRestService.Result[ConfigSettings.SignupConfirmation] = SuccesfullResponce();
            var mainPage = FinishConfirmationClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }

            var accountView = MainClass.OpenAccount(mainPage, mainPage.Accounts[0]);

            var fingerPrintConfirmByPinView = FingerPrintClass.AddFingerPrint(accountView);
            FingerPrintPinViewClass.FinishFingerPrintAdding(fingerPrintConfirmByPinView);

            while (!(CurrentPage is MainView))
            {
                Thread.Sleep(1);
            }

            Assert.IsTrue(CurrentPage is MainView);
        }


        [Test]
        public void AddAccountWithoutPin()
        {
            var page = MainClass.OpenMainPage();
            var tcView = MainClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationClass.EnterCorrectCode(registrationConfirmation);
            TestRestService.Result[ConfigSettings.SignupConfirmation] = SuccesfullResponce();
            var mainPage = FinishConfirmationClass.FinishRegistrationSkipPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
        }
    }
}