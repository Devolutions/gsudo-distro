using Microsoft.IdentityModel.Tokens;
using ThumbprintBundle.Sample;
using Xunit;

namespace ThumbprintBundle.Sample.Tests;

public sealed class BundleVerifierTests
{
    [Fact]
    public void ValidBundle_Verifies_AndContainsTwoEntries()
    {
        var paths = ResolvePaths();
        var jwt = File.ReadAllText(paths.BundlePath).Trim();
        var publicKeyPem = File.ReadAllText(paths.PublicKeyPath);

        var claims = BundleVerifier.VerifyToken(jwt, publicKeyPem);

        Assert.Equal(BundleVerifier.DefaultIssuer, claims.Issuer);
        Assert.Equal(BundleVerifier.DefaultAudience, claims.Audience);
        Assert.Equal(2, claims.Thumbprints.Count);
        Assert.All(claims.Thumbprints, entry =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.X5t));
            Assert.False(string.IsNullOrWhiteSpace(entry.X5tS256));
        });
    }

    [Fact]
    public void BothRepositoryCertificates_AreAllowed()
    {
        var paths = ResolvePaths();
        var jwt = File.ReadAllText(paths.BundlePath).Trim();
        var publicKeyPem = File.ReadAllText(paths.PublicKeyPath);
        var claims = BundleVerifier.VerifyToken(jwt, publicKeyPem);

        Assert.True(BundleVerifier.IsCertificateAllowed(paths.Cert2023Path, claims));
        Assert.True(BundleVerifier.IsCertificateAllowed(paths.Cert2025Path, claims));
    }

    [Fact]
    public void TamperedToken_FailsValidation()
    {
        var paths = ResolvePaths();
        var jwt = File.ReadAllText(paths.BundlePath).Trim();
        var publicKeyPem = File.ReadAllText(paths.PublicKeyPath);

        var parts = jwt.Split('.');
        parts[2] = parts[2][..^1] + (parts[2].EndsWith('A') ? "B" : "A");
        var tampered = string.Join('.', parts);

        Assert.ThrowsAny<SecurityTokenException>(() => BundleVerifier.VerifyToken(tampered, publicKeyPem));
    }

    [Fact]
    public void X5tAndHex_ConvertRoundtrip()
    {
        var paths = ResolvePaths();
        var jwt = File.ReadAllText(paths.BundlePath).Trim();
        var publicKeyPem = File.ReadAllText(paths.PublicKeyPath);
        var claims = BundleVerifier.VerifyToken(jwt, publicKeyPem);

        foreach (var entry in claims.Thumbprints)
        {
            var sha1Hex = BundleVerifier.X5tToWindowsThumbprintHex(entry.X5t);
            var sha1X5tRoundtrip = BundleVerifier.WindowsThumbprintHexToX5t(sha1Hex);
            Assert.Equal(entry.X5t, sha1X5tRoundtrip);

            var sha256Hex = BundleVerifier.X5tS256ToHex(entry.X5tS256);
            var sha256X5tRoundtrip = BundleVerifier.HexToX5tS256(sha256Hex);
            Assert.Equal(entry.X5tS256, sha256X5tRoundtrip);
        }
    }

    private static (string BundlePath, string PublicKeyPath, string Cert2023Path, string Cert2025Path) ResolvePaths()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var bundlePath = Path.Combine(current.FullName, "bundle", "thumbprints.bundle.jwt");
            var publicKeyPath = Path.Combine(current.FullName, "keys", "jwt-public.pem");
            var certsRoot = Path.GetFullPath(Path.Combine(current.FullName, ".."));
            var cert2023Path = Path.Combine(certsRoot, "Devolutions_CodeSign_2023-2026.crt");
            var cert2025Path = Path.Combine(certsRoot, "Devolutions_CodeSign_2025-2028.crt");

            if (File.Exists(bundlePath) && File.Exists(publicKeyPath) && File.Exists(cert2023Path) && File.Exists(cert2025Path))
            {
                return (bundlePath, publicKeyPath, cert2023Path, cert2025Path);
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate prototype artifacts for tests.");
    }
}
