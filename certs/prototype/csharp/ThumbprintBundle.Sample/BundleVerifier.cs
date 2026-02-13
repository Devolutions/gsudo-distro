using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace ThumbprintBundle.Sample;

public static class BundleVerifier
{
    public const string DefaultIssuer = "https://devolutions.net/productinfo/codesign-thumbprints";
    public const string DefaultAudience = "urn:devolutions:update-clients";

    public static ThumbprintBundleClaims VerifyToken(
        string jwt,
        string publicKeyPem,
        string expectedIssuer = DefaultIssuer,
        string expectedAudience = DefaultAudience)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            throw new ArgumentException("JWT cannot be empty.", nameof(jwt));

        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new ArgumentException("Public key PEM cannot be empty.", nameof(publicKeyPem));

        var parts = jwt.Split('.');
        if (parts.Length != 3)
            throw new SecurityTokenException("Invalid JWT format.");

        var headerJson = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[0]));
        using var headerDoc = JsonDocument.Parse(headerJson);
        var alg = headerDoc.RootElement.GetProperty("alg").GetString();
        if (!string.Equals(alg, SecurityAlgorithms.RsaSha256, StringComparison.Ordinal))
            throw new SecurityTokenInvalidAlgorithmException($"Unsupported JWT algorithm: {alg}");

        var signingInput = $"{parts[0]}.{parts[1]}";
        var signatureBytes = Base64UrlEncoder.DecodeBytes(parts[2]);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        var signatureValid = rsa.VerifyData(
            Encoding.UTF8.GetBytes(signingInput),
            signatureBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        if (!signatureValid)
            throw new SecurityTokenInvalidSignatureException("JWT signature validation failed.");

        var payloadJson = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[1]));

        var claims = JsonSerializer.Deserialize<ThumbprintBundleClaims>(payloadJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (claims is null)
            throw new SecurityTokenException("Unable to deserialize thumbprint payload.");

        if (!string.Equals(claims.Issuer, expectedIssuer, StringComparison.Ordinal))
            throw new SecurityTokenInvalidIssuerException($"Invalid issuer: {claims.Issuer}");

        if (!string.Equals(claims.Audience, expectedAudience, StringComparison.Ordinal))
            throw new SecurityTokenInvalidAudienceException($"Invalid audience: {claims.Audience}");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        const long clockSkewSeconds = 120;

        if (claims.ExpiresAt <= now - clockSkewSeconds)
            throw new SecurityTokenExpiredException("JWT has expired.");

        if (claims.NotBefore > now + clockSkewSeconds)
            throw new SecurityTokenNotYetValidException("JWT is not yet valid.");

        if (claims.IssuedAt > now + clockSkewSeconds)
            throw new SecurityTokenInvalidLifetimeException("JWT iat is in the future.");

        return claims;
    }

    public static bool IsCertificateAllowed(string certificatePath, ThumbprintBundleClaims claims)
    {
        var thumbprintHex = ComputeSha1ThumbprintHexFromCertificateFile(certificatePath);
        return claims.Thumbprints.Any(x => string.Equals(x, thumbprintHex, StringComparison.Ordinal));
    }

    public static string ComputeSha1ThumbprintHexFromCertificateFile(string certificatePath)
    {
        using var cert = X509CertificateLoader.LoadCertificateFromFile(certificatePath);
        var sha1Bytes = cert.GetCertHash(HashAlgorithmName.SHA1);
        return Convert.ToHexString(sha1Bytes);
    }
}
