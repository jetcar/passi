
namespace MauiApp2.utils.Services
{
    public class NavigationService : INavigationService
    {
        private List<BaseContentPage> _pages = new List<BaseContentPage>();
        public async Task PushModalSinglePage(BaseContentPage page)
        {
            _pages.Add(page);
            await App.FirstPage.Navigation.PushModalSinglePage(page);
        }

        public async Task NavigateTop()
        {
            _pages.Clear();
            await App.FirstPage.Navigation.NavigateTop();

        }

        public async Task PopModal()
        {
            _pages.RemoveAt(_pages.Count - 1);
            await App.FirstPage.Navigation.PopModal();
        }
    }
}