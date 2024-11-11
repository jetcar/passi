
using ChatViewModel;

namespace MauiApp1;

public class BaseContentPage : ContentPage
{
    public BaseContentPage()
    {
    }

    protected override void OnAppearing()
    {
        ((ChatBaseViewModel)BindingContext).OnAppearing(null, null);
        base.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        ((ChatBaseViewModel)BindingContext).OnDisappearing(null, null);
        base.OnDisappearing();
    }
}