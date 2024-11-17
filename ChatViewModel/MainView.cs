using System.Collections.ObjectModel;
using System.Text;
using IdentityModel.OidcClient;
using MauiCommonServices;
using RestSharp;

namespace ChatViewModel;

public class MainView : ChatBaseViewModel
{
    private string version = "1";
    private readonly OidcClient _client;

    public string Version
    {
        get => version;
        set
        {
            version = value;
            OnPropertyChanged();
        }
    }

    public MainView(OidcClient client)
    {
        _client = client;
        this.Version = CommonApp.Version;
        CounterBtnText = $"Clicked {count} time";
    }

    int count = 0;
    private string _counterBtnText;
    private string _currentAccessToken;

    public async Task OnCounterClicked(object sender, EventArgs e)
    {
        var result = await _client.LoginAsync();

        if (result.IsError)
        {
            //editor.Text = result.Error;
            return;
        }

        _currentAccessToken = result.AccessToken;

        var sb = new StringBuilder(128);

        sb.AppendLine("claims:");
        foreach (var claim in result.User.Claims)
        {
            sb.AppendLine($"{claim.Type}: {claim.Value}");
        }

        sb.AppendLine();
        sb.AppendLine("access token:");
        sb.AppendLine(result.AccessToken);

        if (!string.IsNullOrWhiteSpace(result.RefreshToken))
        {
            sb.AppendLine();
            sb.AppendLine("refresh token:");
            sb.AppendLine(result.RefreshToken);
        }

        //editor.Text = sb.ToString();

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