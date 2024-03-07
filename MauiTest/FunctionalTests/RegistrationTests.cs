using System.Threading;
using MauiTest.TestClasses;
using MauiTest.Tools;
using MauiViewModels;
using NUnit.Framework;

namespace MauiTest.FunctionalTests
{
    public class RegistrationTests : TestBase
    {
        [Test]
        public void AddAccountWithPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            // TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            RegistrationConfirmationTestClass.EnterIncorrectCode(registrationConfirmation);
            Assert.AreEqual(registrationConfirmation.ResponseError, "Code not found");
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            // TestRestService.Result[ConfigSettings.SignupConfirmation] = SuccesfullResponce();
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (!page._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
        }

        [Test]
        public void CancelCodeConfirmPage()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            // TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var mainPage = RegistrationConfirmationTestClass.CancelClick(registrationConfirmation);
            while (!page._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
        }

        [Test]
        public void CancelOnTCPage()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var mainPage = TermsAgreementsTestClass.ClickCancel(tcView);

            while (!page._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 0);
        }

        [Test]
        public void AddAccountWithPinAndFingerPrint()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            // TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            // TestRestService.Result[ConfigSettings.SignupConfirmation] = SuccesfullResponce();
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }

            var accountView = MainTestClass.OpenAccount(mainPage, mainPage.Accounts[0]);

            var fingerPrintConfirmByPinView = FingerPrintTestClass.AddFingerPrint(accountView);
            FingerPrintPinViewClass.FinishFingerPrintAdding(fingerPrintConfirmByPinView);

            while (!(CurrentView is MainView))
            {
                Thread.Sleep(1);
            }

            Assert.IsTrue(CurrentView is MainView);
        }

        [Test]
        public void AddAccountWithPinAndFingerPrintInvalidPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            // TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            //TestRestService.Result[ConfigSettings.SignupConfirmation] = SuccesfullResponce();
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }

            var accountView = MainTestClass.OpenAccount(mainPage, mainPage.Accounts[0]);

            var fingerPrintConfirmByPinView = FingerPrintTestClass.AddFingerPrint(accountView);
            fingerPrintConfirmByPinView = FingerPrintPinViewClass.FinishFingerPrintAddingIncorrectPin(fingerPrintConfirmByPinView);

            Assert.IsNotEmpty(fingerPrintConfirmByPinView.Pin1Error.Text);
            Assert.IsTrue(fingerPrintConfirmByPinView.Pin1Error.HasError);
        }

        [Test]
        public void AddAccountWithoutPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            //TestRestService.Result[ConfigSettings.SignupConfirmation] = SuccesfullResponce();
            var mainPage = FinishConfirmationTestClass.FinishRegistrationSkipPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
        }
    }
}