using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using passi_android;
using passi_android.utils.Services;
using Xamarin.Forms;

namespace AndroidTests.Tools;

internal class TestNavigationService : INavigationService
{
    private List<BaseContentPage> _pages = new List<BaseContentPage>();
    public async Task PushModalSinglePage(BaseContentPage page)
    {
        _pages.Add(page);
        Console.WriteLine(page.ToString());
        if (TestBase.CurrentView != null)
        {
            TestBase.CurrentView.SendDisappearing();
        }
        TestBase.CurrentView = page;
        page.SendAppearing();
    }

    public async Task NavigateTop()
    {
        var page = _pages[0];
        for (int i = _pages.Count - 1; i > 0; i--)
        {
            _pages.RemoveAt(i);
        }
        if (TestBase.CurrentView != null)
            TestBase.CurrentView.SendDisappearing();

        TestBase.CurrentView = page;
        Console.WriteLine(page.ToString());
        page.SendAppearing();

    }

    public async Task PopModal()
    {
        _pages.RemoveAt(_pages.Count - 1);
        var page = _pages.Last();
        if (TestBase.CurrentView != null)
            TestBase.CurrentView.SendDisappearing();

        TestBase.CurrentView = page;
        Console.WriteLine(page.ToString());
        page.SendAppearing();
    }
}