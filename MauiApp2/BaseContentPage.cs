using MauiViewModels;

namespace MauiApp2;

public class BaseContentPage : ContentPage
{
    public BaseContentPage()
    {
    }

    protected override void OnAppearing()
    {
        ((BaseViewModel)BindingContext).OnAppearing(null, null);
        base.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        ((BaseViewModel)BindingContext).OnDisappearing(null, null);
        base.OnDisappearing();
    }
}