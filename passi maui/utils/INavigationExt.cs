namespace passi_maui.utils
{
    public static class NavigationExt
    {
        private static HashSet<Page> ModalStack = new HashSet<Page>();
        public static void NavigateTop(this INavigation navigator)
        {
            var modalStackCount = ModalStack.Count();
            for (int i = 0; i < modalStackCount; i++)
            {
                Shell.Current.GoToAsync("..");

            }
        }

        public static Task PopModal(this INavigation navigator)
        { 
            var lastOrDefault = ModalStack.LastOrDefault();

            ModalStack.Remove(lastOrDefault);
            return Shell.Current.GoToAsync("..");
        }

        public static void PushModalSinglePage(this INavigation navigator, Page page,
            IDictionary<string, object> parameters)
        {
            var modalStackCount = ModalStack.ToList();
            var lastOrDefault = modalStackCount.LastOrDefault();
            if (lastOrDefault != null && lastOrDefault.GetType() == page.GetType())
                return;
            if (lastOrDefault != null)
                lastOrDefault.SendDisappearing();
            ModalStack.Add(page);
            var pagename = page.GetType().Name;
            Shell.Current.GoToAsync(pagename, parameters);

        }
        public static void PushModalSinglePage(this INavigation navigator, Page page)
        {
            var modalStackCount = ModalStack.ToList();
            var lastOrDefault = modalStackCount.LastOrDefault();
            if (lastOrDefault != null && lastOrDefault.GetType() == page.GetType())
                return;
            if (lastOrDefault != null)
                lastOrDefault.SendDisappearing();
            var pagename = page.GetType().Name;

            Shell.Current.GoToAsync(pagename);
        }
    }
}