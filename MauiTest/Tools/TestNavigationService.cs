using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MauiViewModels;
using MauiViewModels.utils.Services;

namespace MauiTest.Tools;

internal class TestNavigationService : INavigationService
{
    public static int navigationsCount = 0;
    private List<BaseContentPage> _pages = new List<BaseContentPage>();

    public async Task PushModalSinglePage(BaseContentPage page)
    {
        _pages.Add(page);
        Console.WriteLine(page.ToString());
        if (TestBase.CurrentView != null)
        {
            TestBase.CurrentView.OnDisappearing();
        }
        TestBase.CurrentView = page;
        Interlocked.Increment(ref navigationsCount);
        page.OnAppearing();
    }

    public async Task NavigateTop()
    {
        var page = _pages[0];
        var pagesCount = _pages.Count;
        for (int i = pagesCount - 1; i > 0; i--)
        {
            _pages.RemoveAt(i);
        }
        if (TestBase.CurrentView != null)
            TestBase.CurrentView.OnDisappearing();

        TestBase.CurrentView = page;
        Console.WriteLine(page.ToString());
        Interlocked.Increment(ref navigationsCount);
        page.OnAppearing();
    }

    public async Task PopModal()
    {
        _pages.RemoveAt(_pages.Count - 1);
        var page = _pages.Last();
        if (TestBase.CurrentView != null)
        {
            var currentView = TestBase.CurrentView;
            Task.Run(() =>
            {
                currentView.OnDisappearing();
            });
        }

        TestBase.CurrentView = page;
        Console.WriteLine(page.ToString());
        Interlocked.Increment(ref navigationsCount);
        page.OnAppearing();
    }

    public void DisplayAlert(string header, string content, string okText)
    {
        throw new NotImplementedException();
    }
}