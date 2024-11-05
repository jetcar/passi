
namespace PassiChat.utils
{
    public static class INavigationExt
    {
        public static async Task NavigateTop(this INavigation navigator)
        {
            var modalStackCount = navigator.NavigationStack.Count;
            for (int i = modalStackCount - 1; i > 1; i--)
            {
                navigator.RemovePage(navigator.NavigationStack[i]);
            }

            var page = await navigator.PopAsync();
        }

        public static async Task PopModal(this INavigation navigator)
        {
            await navigator.PopAsync();
        }

        public static async Task PushModalSinglePage(this INavigation navigator, Page page)
        {
            var loadingPage = navigator.NavigationStack.FirstOrDefault(x => x is LoadingView);
            if (loadingPage != null)
                navigator.RemovePage(loadingPage);

            var modalStackCount = navigator.NavigationStack.ToList();
            var lastOrDefault = modalStackCount.LastOrDefault();
            if (lastOrDefault != null && lastOrDefault.GetType() == page.GetType())
                return;
            await navigator.PushAsync(page);
        }
    }
}