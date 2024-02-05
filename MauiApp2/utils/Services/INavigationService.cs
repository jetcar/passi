namespace MauiApp2.utils.Services
{
    public interface INavigationService
    {
        Task PushModalSinglePage(BaseContentPage page);

        Task NavigateTop();

        Task PopModal();
    }
}