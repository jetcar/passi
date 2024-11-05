namespace MauiCommonServices;

public interface INavigationService
{
    Task PushModalSinglePage(BaseViewModel viewModel);

    Task NavigateTop();

    Task PopModal();

    void DisplayAlert(string header, string content, string okText);
}