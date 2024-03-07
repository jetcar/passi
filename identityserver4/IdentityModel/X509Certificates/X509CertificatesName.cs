﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using PostSharp.Extensibility;

#pragma warning disable 1591

namespace IdentityModel.X509Certificates;

[EditorBrowsable(EditorBrowsableState.Never)]
[Profile(AttributeTargetElements = MulticastTargets.Method)]
public class X509CertificatesName
{
    private readonly StoreLocation _location;
    private readonly StoreName _name;

    public X509CertificatesName(StoreLocation location, StoreName name)
    {
        _location = location;
        _name = name;
    }

    public X509CertificatesFinder Thumbprint => new X509CertificatesFinder(_location, _name, X509FindType.FindByThumbprint);
    public X509CertificatesFinder SubjectDistinguishedName => new X509CertificatesFinder(_location, _name, X509FindType.FindBySubjectDistinguishedName);
    public X509CertificatesFinder SerialNumber => new X509CertificatesFinder(_location, _name, X509FindType.FindBySerialNumber);
    public X509CertificatesFinder IssuerName => new X509CertificatesFinder(_location, _name, X509FindType.FindByIssuerName);
}