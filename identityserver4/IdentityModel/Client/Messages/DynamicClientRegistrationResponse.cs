﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityModel.Client.Extensions;

#pragma warning disable 1591

namespace IdentityModel.Client.Messages;

/// <summary>
/// Models an OpenID Connect dynamic client registration response
/// </summary>
/// <seealso cref="ProtocolResponse" />
public class DynamicClientRegistrationResponse : ProtocolResponse
{
    public string? ErrorDescription => Json.TryGetString("error_description");
    public string? ClientId => Json.TryGetString(OidcConstants.RegistrationResponse.ClientId);
    public string? ClientSecret => Json.TryGetString(OidcConstants.RegistrationResponse.ClientSecret);
    public string? RegistrationAccessToken => Json.TryGetString(OidcConstants.RegistrationResponse.RegistrationAccessToken);
    public string? RegistrationClientUri => Json.TryGetString(OidcConstants.RegistrationResponse.RegistrationClientUri);
    public long? ClientIdIssuedAt => Json.TryGetInt(OidcConstants.RegistrationResponse.ClientIdIssuedAt);
    public long? ClientSecretExpiresAt => Json.TryGetInt(OidcConstants.RegistrationResponse.ClientSecretExpiresAt);
}