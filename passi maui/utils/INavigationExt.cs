namespace passi_maui.utils
{
    public static class NavigationExt
    {
        public static void NavigateTop(this INavigation navigator)
        {
            var modalStackCount = navigator.ModalStack.Count-1;
            for (int i = 0; i < modalStackCount; i++)
            {
                navigator.PopModal();
            }
        }

        public static Task PopModal(this INavigation navigator)
        {
            return navigator.PopModalAsync();
        }

        public static void PushModalSinglePage(this INavigation navigator, Page page)
        {
            var modalStackCount = navigator.ModalStack.ToList();
            var lastOrDefault = modalStackCount.LastOrDefault();
            if (lastOrDefault != null && lastOrDefault.GetType() == page.GetType())
                return;
            navigator.PushModalAsync(page);
        }
    }
}