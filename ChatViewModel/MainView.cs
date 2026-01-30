using System;
using System.Text;
using System.Threading.Tasks;
using MauiCommonServices;

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