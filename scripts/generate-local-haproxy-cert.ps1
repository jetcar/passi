param(
    [string]$BundlePath = "c:\repo\passi\configs\cert\passi_cloud_haproxy.ca-bundle",
    [string[]]$DnsNames = @("localhost", "host.docker.internal"),
    [string]$FriendlyName = "Passi Local HAProxy",
    [switch]$TrustCertificate
)

$ErrorActionPreference = 'Stop'

function Convert-ToPem {
    param(
        [Parameter(Mandatory = $true)]
        [byte[]]$Bytes,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $base64 = [Convert]::ToBase64String($Bytes, [Base64FormattingOptions]::InsertLineBreaks)
    return "-----BEGIN $Label-----`r`n$base64`r`n-----END $Label-----`r`n"
}

$bundleDirectory = Split-Path -Parent $BundlePath
if (-not (Test-Path $bundleDirectory)) {
    New-Item -Path $bundleDirectory -ItemType Directory -Force | Out-Null
}

$existingCertificates = Get-ChildItem -Path Cert:\CurrentUser\My |
Where-Object { $_.FriendlyName -eq $FriendlyName }

foreach ($existingCertificate in $existingCertificates) {
    Remove-Item -Path $existingCertificate.PSPath -Force
}

$existingTrustedCertificates = Get-ChildItem -Path Cert:\CurrentUser\Root |
Where-Object { $_.FriendlyName -eq $FriendlyName }

foreach ($existingTrustedCertificate in $existingTrustedCertificates) {
    Remove-Item -Path $existingTrustedCertificate.PSPath -Force
}

$certificate = New-SelfSignedCertificate `
    -DnsName $DnsNames `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -FriendlyName $FriendlyName `
    -NotAfter (Get-Date).AddYears(5) `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -KeyExportPolicy Exportable `
    -HashAlgorithm SHA256

$certificateBytes = $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
$privateKey = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($certificate)
$privateKeyBytes = $privateKey.Key.Export([System.Security.Cryptography.CngKeyBlobFormat]::Pkcs8PrivateBlob)

$certificatePem = Convert-ToPem -Bytes $certificateBytes -Label 'CERTIFICATE'
$privateKeyPem = Convert-ToPem -Bytes $privateKeyBytes -Label 'PRIVATE KEY'

Set-Content -Path $BundlePath -Value ($privateKeyPem + $certificatePem) -Encoding ascii

$cerPath = [System.IO.Path]::ChangeExtension($BundlePath, '.cer')
[System.IO.File]::WriteAllBytes($cerPath, $certificateBytes)

if ($TrustCertificate) {
    $trustedCertificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($cerPath)
    $trustedStore = New-Object System.Security.Cryptography.X509Certificates.X509Store('Root', 'CurrentUser')
    $trustedStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)

    $existingTrustedCertificate = $trustedStore.Certificates |
    Where-Object { $_.Thumbprint -eq $trustedCertificate.Thumbprint } |
    Select-Object -First 1

    if (-not $existingTrustedCertificate) {
        $trustedStore.Add($trustedCertificate)
    }

    $trustedStore.Close()
}

Write-Host "Generated local HAProxy certificate bundle: $BundlePath"
Write-Host "Generated local certificate file: $cerPath"
if ($TrustCertificate) {
    Write-Host "Trusted certificate in CurrentUser\\Trusted Root Certification Authorities."
}
else {
    Write-Host "Import the .cer into CurrentUser\\Trusted Root Certification Authorities if the browser still does not trust it."
}
Write-Host "DNS names: $($DnsNames -join ', ')"