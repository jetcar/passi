using MauiTest.TestClasses;
using MauiTest.Tools;
using NUnit.Framework;
using System.Threading;
using MauiViewModels;
using MauiViewModels.Menu;

namespace MauiTest.FunctionalTests
{
    public class MenuTests : TestBase
    {
        [Test]
        public void UpdateCertificateWithPin()
        {
            var mainView = MainTestClass.OpenMainPage();
            mainView.Menu_button();

            while (!(CurrentView is MenuViewModel) || !CurrentView.Appeared)
            {
                Thread.Sleep(1);
            }

            var menuView = CurrentView as MenuViewModel;
            Assert.GreaterOrEqual(menuView.Providers.Count, 1);
            menuView.Cell_OnTapped(menuView.Providers[0]);

            while (!(CurrentView is ProviderViewModel) || !CurrentView.Appeared)
            {
                Thread.Sleep(1);
            }

            var providerView = CurrentView as ProviderViewModel;
            providerView.EditButton_OnClicked();

            while (!(CurrentView is EditProviderViewModel) || !CurrentView.Appeared)
            {
                Thread.Sleep(1);
            }

            var editProviderView = CurrentView as EditProviderViewModel;
            editProviderView.SaveButton_OnClicked();

            while (!(CurrentView is ProviderViewModel) || !CurrentView.Appeared)
            {
                Thread.Sleep(1);
            }
        }
    }
}