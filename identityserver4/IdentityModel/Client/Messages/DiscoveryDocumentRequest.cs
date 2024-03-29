﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace IdentityModel.Client.Messages;

/// <summary>
/// Request for OpenID Connect discovery document
/// </summary>
[GoogleTracer.Profile]
public class DiscoveryDocumentRequest : ProtocolRequest
{
    /// <summary>
    /// Gets or sets the policy.
    /// </summary>
    /// <value>
    /// The policy.
    /// </value>
    public DiscoveryPolicy Policy { get; set; } = new();
}