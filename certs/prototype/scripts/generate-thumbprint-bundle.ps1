param(
    [string[]]$CertPaths = @(
        (Join-Path $PSScriptRoot "..\..\Devolutions_CodeSign_2023-2026.crt"),
        (Join-Path $PSScriptRoot "..\..\Devolutions_CodeSign_2025-2028.crt")
    ),
    [string]$Issuer = "https://devolutions.net/productinfo/codesign-thumbprints",
    [string]$Audience = "urn:devolutions:update-clients",
    [int]$ValidityDays = 7,
    [string]$Version = "1",
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-Base64Url {
    param([byte[]]$Bytes)

    $b64 = [Convert]::ToBase64String($Bytes)
    $b64 = $b64.TrimEnd('=').Replace('+', '-').Replace('/', '_')
    return $b64
}

function ConvertTo-Pem {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][byte[]]$DerBytes
    )

    $b64 = [Convert]::ToBase64String($DerBytes)
    $lines = for ($index = 0; $index -lt $b64.Length; $index += 64) {
        $take = [Math]::Min(64, $b64.Length - $index)
        $b64.Substring($index, $take)
    }

    $joined = ($lines -join "`n")
    return "-----BEGIN $Label-----`n$joined`n-----END $Label-----`n"
}

$prototypeRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$bundleDir = Join-Path $prototypeRoot "bundle"
$keysDir = Join-Path $prototypeRoot "keys"

New-Item -ItemType Directory -Force -Path $bundleDir | Out-Null
New-Item -ItemType Directory -Force -Path $keysDir | Out-Null

$privateKeyPath = Join-Path $keysDir "jwt-private.pem"
$publicKeyPath = Join-Path $keysDir "jwt-public.pem"

if ($Force -or -not (Test-Path $privateKeyPath) -or -not (Test-Path $publicKeyPath)) {
    $rsa = [System.Security.Cryptography.RSA]::Create(3072)
    try {
        $privateDer = $rsa.ExportPkcs8PrivateKey()
        $publicDer = $rsa.ExportSubjectPublicKeyInfo()

        $privatePem = ConvertTo-Pem -Label "PRIVATE KEY" -DerBytes $privateDer
        $publicPem = ConvertTo-Pem -Label "PUBLIC KEY" -DerBytes $publicDer

        [System.IO.File]::WriteAllText($privateKeyPath, $privatePem, [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText($publicKeyPath, $publicPem, [System.Text.UTF8Encoding]::new($false))
    }
    finally {
        $rsa.Dispose()
    }
}

$privatePemContent = [System.IO.File]::ReadAllText($privateKeyPath)
$rsaSigner = [System.Security.Cryptography.RSA]::Create()
$rsaSigner.ImportFromPem($privatePemContent)

$publicParameters = $rsaSigner.ExportParameters($false)
$spkiHash = [System.Security.Cryptography.SHA256]::HashData($rsaSigner.ExportSubjectPublicKeyInfo())
$kidBytes = $spkiHash[0..15]
$kid = ConvertTo-Base64Url -Bytes $kidBytes

$thumbprints = @()
foreach ($certPath in $CertPaths) {
    $resolvedCertPath = (Resolve-Path $certPath).Path
    $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($resolvedCertPath)

    try {
        $sha1Bytes = $cert.GetCertHash([System.Security.Cryptography.HashAlgorithmName]::SHA1)
        $sha256Bytes = $cert.GetCertHash([System.Security.Cryptography.HashAlgorithmName]::SHA256)

        $thumbprints += [ordered]@{
            x5t = ConvertTo-Base64Url -Bytes $sha1Bytes
            "x5t#S256" = ConvertTo-Base64Url -Bytes $sha256Bytes
        }
    }
    finally {
        $cert.Dispose()
    }
}

$now = [DateTimeOffset]::UtcNow
$payload = [ordered]@{
    iss = $Issuer
    aud = $Audience
    iat = $now.ToUnixTimeSeconds()
    nbf = $now.ToUnixTimeSeconds()
    exp = $now.AddDays($ValidityDays).ToUnixTimeSeconds()
    ver = $Version
    thumbprints = $thumbprints
}

$header = [ordered]@{
    alg = "RS256"
    typ = "JWT"
    kid = $kid
}

$headerJson = $header | ConvertTo-Json -Depth 10 -Compress
$payloadJson = $payload | ConvertTo-Json -Depth 10 -Compress
$payloadPrettyJson = $payload | ConvertTo-Json -Depth 10

$headerB64 = ConvertTo-Base64Url -Bytes ([System.Text.Encoding]::UTF8.GetBytes($headerJson))
$payloadB64 = ConvertTo-Base64Url -Bytes ([System.Text.Encoding]::UTF8.GetBytes($payloadJson))
$signingInput = "$headerB64.$payloadB64"
$signatureBytes = $rsaSigner.SignData([System.Text.Encoding]::UTF8.GetBytes($signingInput), [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
$signatureB64 = ConvertTo-Base64Url -Bytes $signatureBytes
$jwt = "$signingInput.$signatureB64"

$jwks = [ordered]@{
    keys = @(
        [ordered]@{
            kty = "RSA"
            use = "sig"
            alg = "RS256"
            kid = $kid
            n = ConvertTo-Base64Url -Bytes $publicParameters.Modulus
            e = ConvertTo-Base64Url -Bytes $publicParameters.Exponent
        }
    )
}

$jwksJson = $jwks | ConvertTo-Json -Depth 10

[System.IO.File]::WriteAllText((Join-Path $bundleDir "thumbprints.payload.json"), $payloadPrettyJson + "`n", [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $bundleDir "thumbprints.bundle.jwt"), $jwt + "`n", [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $bundleDir "jwks.public.json"), $jwksJson + "`n", [System.Text.UTF8Encoding]::new($false))

$manifest = [ordered]@{
    generatedAtUtc = $now.ToString("yyyy-MM-ddTHH:mm:ssZ")
    issuer = $Issuer
    audience = $Audience
    version = $Version
    kid = $kid
    certCount = $thumbprints.Count
}
$manifestJson = $manifest | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText((Join-Path $bundleDir "thumbprints.manifest.json"), $manifestJson + "`n", [System.Text.UTF8Encoding]::new($false))

Write-Host "Generated signed thumbprint bundle"
Write-Host "  Private key: $privateKeyPath"
Write-Host "  Public key:  $publicKeyPath"
Write-Host "  Payload:     $(Join-Path $bundleDir 'thumbprints.payload.json')"
Write-Host "  JWT:         $(Join-Path $bundleDir 'thumbprints.bundle.jwt')"
Write-Host "  JWKS:        $(Join-Path $bundleDir 'jwks.public.json')"
