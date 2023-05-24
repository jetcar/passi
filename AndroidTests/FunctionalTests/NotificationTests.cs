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
            var page = MainClass.OpenMainPage();
            var tcView = MainClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewClass.OpenNotificationPage(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            var confirmByPinView = NofiticationViewClass.ChooseColorWithPin(notificationPage, Xamarin.Forms.Color.Blue);

            var mainPage2 = ConfirmByPinClass.ConfirmByPin(confirmByPinView);
            Assert.IsNotNull(mainPage2);
        }        
        [Test]
        public void NotificationAccountWithFingerPrint()
        {
            var page = MainClass.OpenMainPage();
            var tcView = MainClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            //add fingerprint

            var accountView = MainClass.OpenAccount(mainPage, mainPage.Accounts[0]);
            var fingerPrintConfirmByPinView = FingerPrintClass.AddFingerPrint(accountView);
            FingerPrintPinViewClass.FinishFingerPrintAdding(fingerPrintConfirmByPinView);



            var notificationPage =
                NofiticationViewClass.OpenNotificationPage(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            var confirmByPinView = NofiticationViewClass.ChooseColorWithPin(notificationPage, Xamarin.Forms.Color.Blue);

            var mainPage2 = ConfirmByPinClass.ConfirmByFingerprint(confirmByPinView);
            Assert.IsNotNull(mainPage2);
        }        
        [Test]
        public void CancelClickConfirmByPin()
        {
            var page = MainClass.OpenMainPage();
            var tcView = MainClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewClass.OpenNotificationPage(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            var confirmByPinView = NofiticationViewClass.ChooseColorWithPin(notificationPage, Xamarin.Forms.Color.Blue);
            confirmByPinView.Cancel_OnClicked(new Button(),null);
            while (!(CurrentPage is MainView))
            {
                Thread.Sleep(1);
            }
            var mainPage2 = CurrentPage as MainView;
            Assert.IsNotNull(mainPage2);

        }        
        [Test]
        public void CancelNotification()
        {
            var page = MainClass.OpenMainPage();
            var tcView = MainClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewClass.OpenNotificationPage(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            notificationPage.Cancel_OnClicked(new Button(),null);
            while (!(CurrentPage is MainView))
            {
                Thread.Sleep(1);
            }
            var mainPage2 = CurrentPage as MainView;
            Assert.IsNotNull(mainPage2);

        }
        [Test]
        public void ConfirmByPinTimeout()
        {
            var page = MainClass.OpenMainPage();
            var tcView = MainClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationClass.FinishRegistrationWithPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);
            var notificationPage =
                NofiticationViewClass.OpenNotificationPage(mainPage.Accounts[0], Color.blue, 1);
            Assert.AreEqual(notificationPage.TimeLeft, "0");

            var confirmByPinView = NofiticationViewClass.ChooseColorWithPin(notificationPage, Xamarin.Forms.Color.Blue);

            while (!(CurrentPage is MainView))
            {
                Thread.Sleep(1);
            }
            var mainPage2 = CurrentPage as MainView;
            Assert.IsNotNull(mainPage2);
        }


        [Test]
        public void NotificatioAccountWithoutPin()
        {
            var page = MainClass.OpenMainPage();
            var tcView = MainClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = TestBase.SuccesfullResponce();
            var registrationConfirmation = AddAccountClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationClass.FinishRegistrationSkipPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            var notificationPage =
                NofiticationViewClass.OpenNotificationPage(mainPage.Accounts[0], Color.blue, 10);
            Assert.AreEqual(notificationPage.TimeLeft, "9");

            var mainPage2 = NofiticationViewClass.ChooseColorWithoutPin(notificationPage, Xamarin.Forms.Color.Blue);

            Assert.IsNotNull(mainPage2);

        }
        

        [Test]
        public void NotificationTimeoutTest()
        {
            var page = MainClass.OpenMainPage();
            var tcView = MainClass.ClickAddAccount(page);
            var addAccountView = TermsAgreementsClass.ClickAgree(tcView);
            addAccountView.EmailText = "test@test.ee";
            TestRestService.Result[ConfigSettings.Signup] = SuccesfullResponce();
            var registrationConfirmation = AddAccountClass.ClickConfirm(addAccountView);
            var finishConfirmation = RegistrationConfirmationClass.EnterCorrectCode(registrationConfirmation);
            var mainPage = FinishConfirmationClass.FinishRegistrationSkipPin(finishConfirmation);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(mainPage.Accounts.Count, 1);

            var notificationPage =
                NofiticationViewClass.OpenNotificationPage(mainPage.Accounts[0], Color.blue, 1);
            Assert.AreEqual(notificationPage.TimeLeft, "0");

            while (!(CurrentPage is MainView))
            {
                Thread.Sleep(1);
            }
            var mainPage2 = CurrentPage as MainView;
            Assert.IsNotNull(mainPage2);

        }
    }
}