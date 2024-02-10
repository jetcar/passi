using MauiViewModels.utils.Services;

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

        public Task PushModalSinglePage(MauiViewModels.BaseViewModel page)
        {
            throw new NotImplementedException();
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

        public void DisplayAlert(string header, string content, string okText)
        {
            throw new NotImplementedException();
        }
    }
}