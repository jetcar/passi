using System;

namespace IdentityServer;

internal static class StringExtensions
{
    public static bool IsPresent(this string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    public static string CleanUrlPath(this string url)
    {
        if (String.IsNullOrWhiteSpace(url)) url = "/";

        if (url != "/" && url.EndsWith("/"))
        {
            url = url.Substring(0, url.Length - 1);
        }

        return url;
    }

    public static string AddQueryString(this string url, string query)
    {
        if (!url.Contains("?"))
        {
            url += "?";
        }
        else if (!url.EndsWith("&"))
        {
            url += "&";
        }

        return url + query;
    }

    public static string GetOrigin(this string url)
    {
        if (url != null)
        {
            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch (Exception)
            {
                return null;
            }

            if (uri.Scheme == "http" || uri.Scheme == "https")
            {
                return $"{uri.Scheme}://{uri.Authority}";
            }
        }

        return null;
    }
}