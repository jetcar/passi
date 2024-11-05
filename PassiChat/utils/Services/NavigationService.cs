using MauiCommonServices;

namespace PassiChat.utils.Services
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

        public async Task PushModalSinglePage(BaseViewModel viewModel)
        {
            var pageType = viewModel.GetType().FullName;
            pageType = pageType.Replace("MauiViewModels", "MauiApp2").Replace("ViewModel", "View");
            var unwrap = Activator.CreateInstance(App.FirstPage.GetType().Assembly.FullName, pageType).Unwrap();
            var page = (BaseContentPage)unwrap;
            page.BindingContext = viewModel;
            //page.Appearing += viewModel.OnAppearing;
            //page.Disappearing += viewModel.OnDisappearing;
            PushModalSinglePage(page);
        }

        public async Task NavigateTop()
        {
            //foreach (var page in _pages)
            //{
            //    page.Appearing -= ((BaseViewModel)page.BindingContext).OnAppearing;
            //    page.Disappearing -= ((BaseViewModel)page.BindingContext).OnDisappearing;
            //}
            _pages.Clear();
            var navigation = App.FirstPage.Navigation;
            await navigation.NavigateTop();
        }

        public async Task PopModal()
        {
            var page = _pages[_pages.Count - 1];
            page.Appearing -= ((BaseViewModel)page.BindingContext).OnAppearing;
            page.Disappearing -= ((BaseViewModel)page.BindingContext).OnDisappearing;

            _pages.RemoveAt(_pages.Count - 1);
            var navigation = App.FirstPage.Navigation;
            await navigation.PopModal();
        }

        public void DisplayAlert(string header, string content, string okText)
        {
            App.FirstPage.DisplayAlert(header, content, okText);
        }
    }
}