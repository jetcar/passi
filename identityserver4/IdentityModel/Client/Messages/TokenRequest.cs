﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace IdentityModel.Client.Messages;

/// <summary>
/// Request for token
/// </summary>
/// <seealso cref="ProtocolRequest" />
[GoogleTracer.Profile]
public class TokenRequest : ProtocolRequest
{
    /// <summary>
    /// Gets or sets the type of the grant.
    /// </summary>
    /// <value>
    /// The type of the grant.
    /// </value>
    public string GrantType { get; set; } = default!;
}