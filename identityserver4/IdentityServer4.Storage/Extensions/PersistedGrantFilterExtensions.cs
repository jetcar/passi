// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using IdentityServer4.Storage.Stores;
using PostSharp.Extensibility;

namespace IdentityServer4.Storage.Extensions
{
    /// <summary>
    /// Extensions for PersistedGrantFilter.
    /// </summary>
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public static class PersistedGrantFilterExtensions
    {
        /// <summary>
        /// Validates the PersistedGrantFilter and throws if invalid.
        /// </summary>
        /// <param name="filter"></param>
        public static void Validate(this PersistedGrantFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            if (String.IsNullOrWhiteSpace(filter.ClientId) &&
                String.IsNullOrWhiteSpace(filter.SessionId) &&
                String.IsNullOrWhiteSpace(filter.SubjectId) &&
                String.IsNullOrWhiteSpace(filter.Type))
            {
                throw new ArgumentException("No filter values set.", nameof(filter));
            }
        }
    }
}