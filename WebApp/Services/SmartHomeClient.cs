using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConfigurationManager;

namespace WebApp.Services;

/// <summary>A fixed API boundary. Never retries writes or follows redirects carrying a token.</summary>
public sealed class SmartHomeClient(HttpClient client, AppSetting settings)
{
    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token,
        JsonElement? body, CancellationToken cancellationToken)
    {
        // The public certificate is trusted by the OS. Do not reuse the legacy internal TLS bypass.
        var root = settings["SmartHomeApiBaseUrl"] ??
                   (settings["PublicUrlBase"] ?? settings["IdentityUrlBase"])?.TrimEnd('/') + "/passiapi";
        if (!Uri.TryCreate(root?.TrimEnd('/') + "/api/smarthome/", UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(baseUri.UserInfo) ||
            !string.IsNullOrEmpty(baseUri.Query) || !string.IsNullOrEmpty(baseUri.Fragment))
            throw new InvalidOperationException("SmartHomeApiBaseUrl must be an HTTPS API origin/path");
        using var request = new HttpRequestMessage(method, new Uri(baseUri, path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body.HasValue) request.Content = new StringContent(body.Value.GetRawText(), Encoding.UTF8, "application/json");
        return await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
    }
}
