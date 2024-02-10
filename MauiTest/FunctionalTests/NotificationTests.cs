using System;
using System.Threading;
using AppConfig;
using MauiTest.TestClasses;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.utils.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Graphics;
using NUnit.Framework;

namespace MauiTest.FunctionalTests
{
    public class NotificationTests : TestBase
    {
        [Test]
        public void NotificationAccountWithPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out var color);
            Assert.GreaterOrEqual(Convert.ToInt32(notificationPage.TimeLeft), 100);

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, color);

            var mainPage2 = ConfirmByPinTestClass.ConfirmByPin(confirmByPinView);
            Assert.IsNotNull(mainPage2);
        }

        [Test]
        public void NotificationAccountWithInvalidPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out var color);
            Assert.GreaterOrEqual(Convert.ToInt32(notificationPage.TimeLeft), 100);

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, color);

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
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out var color);
            Assert.GreaterOrEqual(Convert.ToInt32(notificationPage.TimeLeft), 100);

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, color);

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
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out var color);
            Assert.GreaterOrEqual(Convert.ToInt32(notificationPage.TimeLeft), 100);

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, color);

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
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
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
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out var color);
            Assert.GreaterOrEqual(Convert.ToInt32(notificationPage.TimeLeft), 100);

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, color);

            var mainPage2 = ConfirmByPinTestClass.ConfirmByFingerprint(confirmByPinView);
            Assert.IsNotNull(mainPage2);
        }

        [Test]
        public void NotificationAccountWithFingerPrintNoPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
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
            Assert.AreEqual(App.Services.GetService<ISecureRepository>().GetAccount(mainPage.Accounts[0].Guid).HaveFingerprint, true);

            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out var color);
            Assert.GreaterOrEqual(Convert.ToInt32(notificationPage.TimeLeft), 100);

            TestNavigationService.navigationsCount = 0;
            var confirmView = NofiticationViewTestClass.ChooseColorWithoutPinFingerPrint(notificationPage, color);
            TestBase.TouchFingerPrintWithGoodResult();
            Assert.AreEqual(1, TestNavigationService.navigationsCount);
            while (!(TestBase.CurrentView is MainView))
            {
                Thread.Sleep(1);
            }
            var mainPage2 = TestBase.CurrentView as MainView;
            if (mainPage2._loadAccountTask != null)
                while (!mainPage2._loadAccountTask.IsCompleted)
                {
                    Thread.Sleep(1);
                }
            Assert.IsNotNull(mainPage2);
        }

        [Test]
        public void CancelClickConfirmByPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out Color color);
            Assert.GreaterOrEqual(Convert.ToInt32(notificationPage.TimeLeft), 80);

            var confirmByPinView = NofiticationViewTestClass.ChooseColorWithPin(notificationPage, color);
            confirmByPinView.Cancel_OnClicked();
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
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out var color);
            Assert.GreaterOrEqual(Convert.ToInt32(notificationPage.TimeLeft), 100);

            notificationPage.Cancel_OnClicked();
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
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out var color);
            Assert.GreaterOrEqual(Convert.ToInt32(notificationPage.TimeLeft), 100);
            notificationPage.Message.ExpirationTime = DateTime.UtcNow;
            Thread.Sleep(5000);
            Assert.AreEqual(TestNavigationService.AlertMessage, "Session Expired");
            var mainPage2 = CurrentView as MainView;
            Assert.IsNotNull(mainPage2);
        }

        [Test]
        public void NotificatioAccountWithoutPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationSkipPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out var color);
            //Assert.AreEqual(notificationPage.TimeLeft, "9");

            TestNavigationService.navigationsCount = 0;
            var mainPage2 = NofiticationViewTestClass.ChooseColorWithoutPin(notificationPage, color);
            Assert.AreEqual(2, TestNavigationService.navigationsCount);

            Assert.IsNotNull(mainPage2);
        }

        [Test]
        public void SameNotificatioAccountWithoutPin()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
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
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out var color);

            TestNavigationService.navigationsCount = 0;
            var mainPage2 = NofiticationViewTestClass.ChooseColorWithoutPin(notificationPage, color);
            Assert.AreEqual(2, TestNavigationService.navigationsCount);

            Assert.IsNotNull(mainPage2);

            var mainPage3 =
                NofiticationViewTestClass.PollExistingSessionId(mainPage.Accounts[0], WebApiDto.Auth.Color.blue, 10, newGuid);
        }

        [Test]
        public void NotificationTimeoutTest()
        {
            var page = MainTestClass.OpenMainPage();
            var tcView = MainTestClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsTestClass.ClickAgree(tcView);
            addAccountView.EmailText = $"{GetRandomString(6)}@test.ee";
            //TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountTestClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationTestClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationTestClass.FinishRegistrationSkipPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            var notificationPage =
                NofiticationViewTestClass.PollOpenSessions(mainPage.Accounts[0], out var color);
            notificationPage.Message.ExpirationTime = DateTime.UtcNow;
            Thread.Sleep(5000);
            Assert.LessOrEqual(Convert.ToInt32(notificationPage.TimeLeft), 0);

            while (!(CurrentView is MainView))
            {
                Thread.Sleep(1);
            }
            var mainPage2 = CurrentView as MainView;
            Assert.IsNotNull(mainPage2);
        }
    }
}