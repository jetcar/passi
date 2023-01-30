﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.IO;

namespace IdentityModel.UnitTests
{
    internal static class FileName
    {
        public static string Create(string name)
        {
            var fullName = Path.Combine(System.AppContext.BaseDirectory, "documents", name);

            return fullName;
        }
    }
}
