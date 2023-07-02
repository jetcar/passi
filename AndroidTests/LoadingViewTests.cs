
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using AndroidTests.Tools;
using AppConfig;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using passi_android;
using passi_android.Main;
using passi_android.Menu;
using passi_android.Tools;
using passi_android.utils.Services;
using passi_android.ViewModels;
using WebApiDto.Auth;
using Xamarin.Forms;

namespace AndroidTests
{
    public class LoadingViewTests : TestBase
    {


        [Test, Timeout(100000)]
        public void LoadingViewExpired()
        {
            App.SkipLoadingTimer = false;
            TestBase.Navigation.PushModalSinglePage(new MainView());
            TestBase.Navigation.PushModalSinglePage(new LoadingView(() =>
            {
                Task.Delay(2000);
            }, 1000));

            while (!TestBase.CurrentView.Appeared || !(TestBase.CurrentView is LoadingView))
            {
                Thread.Sleep(1);
            }

            while (!TestBase.CurrentView.Appeared || !(TestBase.CurrentView is MainView))
            {
                Thread.Sleep(1);
            }

        }
        [Test, Timeout(100000)]
        public void LoadingViewNavigateBack()
        {
            TestBase.Navigation.PushModalSinglePage(new MainView());
            TestBase.Navigation.PushModalSinglePage(new LoadingView(() =>
            {
                App.Services.GetService<INavigationService>().PopModal();
            }));

            while (!TestBase.CurrentView.Appeared || !(TestBase.CurrentView is MainView))
            {
                Thread.Sleep(1);
            }

        }



    }
}