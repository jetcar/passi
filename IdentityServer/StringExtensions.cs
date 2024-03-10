using IdentityServer4.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;

namespace IdentityServer;

[GoogleTracer.Profile]
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