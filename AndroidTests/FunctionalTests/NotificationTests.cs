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
using passi_android.Tools;
using passi_android.utils.Services;
using RestSharp;
using Xamarin.Forms;
using Color = WebApiDto.Auth.Color;

namespace AndroidTests.FunctionalTests
{
    public class NotificationTests : TestBase
    {

        [Test]
        public void NotificationAccountWithPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, Xamarin.Forms.Color.Blue);

            var mainPage2 = ConfirmByPinTestClass.ConfirmByPin(confirmByPinView);
            Assert.IsNotNull(mainPage2);
        }
        [Test]
        public void NotificationAccountWithInvalidPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, Xamarin.Forms.Color.Blue);

            confirmByPinView = ConfirmByPinTestClass.ConfirmByIncorrectPin(confirmByPinView);
            Assert.IsNotNull(confirmByPinView);
            Assert.IsNotEmpty(confirmByPinView.Pin1Error.Text);
            Assert.IsTrue(confirmByPinView.Pin1Error.HasError);
        }
        [Test]
        public void NotificationAccountBadResponse()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, Xamarin.Forms.Color.Blue);

            confirmByPinView = ConfirmByPinTestClass.ConfirmByPinBadResponse(confirmByPinView);
            Assert.IsNotNull(confirmByPinView);
            Assert.IsNotEmpty(confirmByPinView.ResponseError);
        }
        [Test]
        public void NotificationAccountNetworkError()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, Xamarin.Forms.Color.Blue);

            confirmByPinView = ConfirmByPinTestClass.ConfirmByPinNetworkError(confirmByPinView);
            Assert.IsNotNull(confirmByPinView);
            Assert.IsNotEmpty(confirmByPinView.ResponseError);
        }
        [Test]
        public void NotificationAccountWithFingerPrintAndPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            //add fingerprint

            var accountView = MainTestClass.OpenAccount(mainPage, mainPage.Accounts[0]);
            var fingerPrintConfirmByPinView = FingerPrintTestClass.AddFingerPrint(accountView);
            FingerPrintPinViewClass.FinishFingerPrintAdding(fingerPrintConfirmByPinView);



            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, Xamarin.Forms.Color.Blue);

            var mainPage2 = ConfirmByPinTestClass.ConfirmByFingerprint(confirmByPinView);
            Assert.IsNotNull(mainPage2);
        }
        [Test]
        public void NotificationAccountWithFingerPrintNoPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationSkipPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            //add fingerprint

            var accountView = MainTestClass.OpenAccount(mainPage, mainPage.Accounts[0]);
            FingerPrintTestClass.AddFingerPrintNoPin(accountView);

            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            TestNavigationService.navigationsCount = 0; 
            var mainPage2 = NofiticationViewTestClass.ChooseColorWithoutPin(notificationPage, Xamarin.Forms.Color.Blue);
            TestBase.TouchFingerPrintWithGoodResult();
            Assert.AreEqual(2, TestNavigationService.navigationsCount);

            Assert.IsNotNull(mainPage2);
        }
        [Test]
        public void CancelClickConfirmByPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, Xamarin.Forms.Color.Blue);
            confirmByPinView.Cancel_OnClicked(new Button(), null);
            while (!(CurrentView is MainView))
            {
                Thread.Sleep(1);
            }
            var mainPage2 = CurrentView as MainView;
            Assert.IsNotNull(mainPage2);

        }
        [Test]
        public void CancelNotification()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            notificationPage.Cancel_OnClicked(new Button(), null);
            while (!(CurrentView is MainView))
            {
                Thread.Sleep(1);
            }
            var mainPage2 = CurrentView as MainView;
            Assert.IsNotNull(mainPage2);

        }
        [Test]
        public void ConfirmByPinTimeout()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 1);
            Assert.AreEqual(notificationPage.TimeLeft, "0");

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, Xamarin.Forms.Color.Blue);

            while (!(CurrentView is MainView))
            {
                Thread.Sleep(1);
            }
            var mainPage2 = CurrentView as MainView;
            Assert.IsNotNull(mainPage2);
        }


        [Test]
        public void NotificatioAccountWithoutPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationSkipPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 10);
            //Assert.AreEqual(notificationPage.TimeLeft, "9");

            TestNavigationService.navigationsCount = 0; 
            var mainPage2 = NofiticationViewTestClass.ChooseColorWithoutPin(notificationPage, Xamarin.Forms.Color.Blue);
            Assert.AreEqual(2, TestNavigationService.navigationsCount);

            Assert.IsNotNull(mainPage2);

        }

        [Test]
        public void SameNotificatioAccountWithoutPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationSkipPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            var newGuid = Guid.NewGuid();
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 10, newGuid);

            TestNavigationService.navigationsCount = 0; 
            var mainPage2 = NofiticationViewTestClass.ChooseColorWithoutPin(notificationPage, Xamarin.Forms.Color.Blue);
            Assert.AreEqual(2, TestNavigationService.navigationsCount);

            Assert.IsNotNull(mainPage2);

            var mainPage3 =
                NofiticationViewTestClass.PollExistingSessionId(mainPage.Accounts[0], Color.blue, 10, newGuid);


        }


        [Test]
        public void NotificationTimeoutTest()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationSkipPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], Color.blue, 1);
            Assert.AreEqual(notificationPage.TimeLeft, "0");

            while (!(CurrentView is MainView))
            {
                Thread.Sleep(1);
            }
            var mainPage2 = CurrentView as MainView;
            Assert.IsNotNull(mainPage2);

        }
    }
}