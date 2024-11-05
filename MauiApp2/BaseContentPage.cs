using MauiViewModels;

namespace MauiApp2;

public class BaseContentPage : ContentPage
{
    public BaseContentPage()
    {
    }

    protected override void OnAppearing()
    {
        ((PassiBaseViewModel)BindingContext).OnAppearing(null, null);
        base.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        ((PassiBaseViewModel)BindingContext).OnDisappearing(null, null);
        base.OnDisappearing();
    }
}