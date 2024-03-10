// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using GoogleTracer;

namespace IdentityModel.Internal;

[Profile]
internal static class InternalStringExtensions
{
    public static bool IsMissing(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static bool IsPresent(this string value)
    {
        return !(value.IsMissing());
    }

    public static string EnsureTrailingSlash(this string url)
    {
        if (!url.EndsWith("/"))
        {
            return url + "/";
        }

        return url;
    }

    public static string RemoveTrailingSlash(this string url)
    {
        if (url == null) throw new ArgumentNullException(nameof(url));

        if (url.EndsWith("/"))
        {
            url = url.Substring(0, url.Length - 1);
        }

        return url;
    }
}