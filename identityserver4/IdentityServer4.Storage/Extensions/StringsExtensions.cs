// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Diagnostics;
using PostSharp.Extensibility;

namespace IdentityServer4.Storage.Extensions
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    internal static class StringExtensions
    {
        public static bool IsMissing(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static bool IsPresent(this string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}