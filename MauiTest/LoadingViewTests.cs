using System.Threading;
using System.Threading.Tasks;
using MauiCommonServices;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.Tools;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace MauiTest
{
    public class LoadingViewTests : TestBase
    {
        [Test, MaxTime(100000)]
        public void LoadingViewExpired()
        {
            CommonApp.SkipLoadingTimer = false;
            TestBase.Navigation.PushModalSinglePage(new MainView());
            TestBase.Navigation.PushModalSinglePage(new LoadingViewModel(() =>
            {
                Task.Delay(2000);
            }, 1000));

            while (!TestBase.CurrentView.Appeared || !(TestBase.CurrentView is LoadingViewModel))
            {
                Thread.Sleep(1);
            }

            while (!TestBase.CurrentView.Appeared || !(TestBase.CurrentView is MainView))
            {
                Thread.Sleep(1);
            }
        }

        [Test, MaxTime(100000)]
        public void LoadingViewNavigateBack()
        {
            TestBase.Navigation.PushModalSinglePage(new MainView());
            TestBase.Navigation.PushModalSinglePage(new LoadingViewModel(() =>
            {
                CommonApp.Services.GetService<INavigationService>().PopModal();
            }));

            while (!TestBase.CurrentView.Appeared || !(TestBase.CurrentView is MainView))
            {
                Thread.Sleep(1);
            }
        }
    }
}