namespace MauiApp2.utils.Services
{
    public class NavigationService : INavigationService
    {
        private List<BaseContentPage> _pages = new List<BaseContentPage>();

        public async Task PushModalSinglePage(BaseContentPage page)
        {
            _pages.Add(page);
            var navigation = App.FirstPage.Navigation;
            await navigation.PushModalSinglePage(page);
        }

        public async Task NavigateTop()
        {
            _pages.Clear();
            var navigation = App.FirstPage.Navigation;
            await navigation.NavigateTop();
        }

        public async Task PopModal()
        {
            _pages.RemoveAt(_pages.Count - 1);
            var navigation = App.FirstPage.Navigation;
            await navigation.PopModal();
        }
    }
}