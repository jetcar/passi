using System.Collections.ObjectModel;
using MauiCommonServices;
using RestSharp;

namespace ChatViewModel;

public class MainView : ChatBaseViewModel
{
    private string version = "1";

    public string Version
    {
        get => version;
        set
        {
            version = value;
            OnPropertyChanged();
        }
    }

    public MainView()
    {
        this.Version = CommonApp.Version;
        CounterBtnText = $"Clicked {count} time";
    }

    int count = 0;
    private string _counterBtnText;

    public void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtnText = $"Clicked {count} time";
        else
            CounterBtnText = $"Clicked {count} times";

    }

    public string CounterBtnText
    {
        get => _counterBtnText;
        set
        {
            if (value == _counterBtnText) return;
            _counterBtnText = value;
            OnPropertyChanged();
        }
    }
}