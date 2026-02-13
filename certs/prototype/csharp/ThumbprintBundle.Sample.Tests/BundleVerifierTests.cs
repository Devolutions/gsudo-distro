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
        Assert.All(claims.Thumbprints, entry => Assert.Matches("^[0-9A-F]{40}$", entry));

        var newestThumbprint = BundleVerifier.ComputeSha1ThumbprintHexFromCertificateFile(paths.Cert2025Path);
        var olderThumbprint = BundleVerifier.ComputeSha1ThumbprintHexFromCertificateFile(paths.Cert2023Path);
        Assert.Equal(newestThumbprint, claims.Thumbprints[0]);
        Assert.Equal(olderThumbprint, claims.Thumbprints[1]);
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
    public void ComputedSha1Thumbprints_ArePresentInBundle()
    {
        var paths = ResolvePaths();
        var jwt = File.ReadAllText(paths.BundlePath).Trim();
        var publicKeyPem = File.ReadAllText(paths.PublicKeyPath);
        var claims = BundleVerifier.VerifyToken(jwt, publicKeyPem);

        var cert2025Thumbprint = BundleVerifier.ComputeSha1ThumbprintHexFromCertificateFile(paths.Cert2025Path);
        var cert2023Thumbprint = BundleVerifier.ComputeSha1ThumbprintHexFromCertificateFile(paths.Cert2023Path);

        Assert.Contains(cert2025Thumbprint, claims.Thumbprints);
        Assert.Contains(cert2023Thumbprint, claims.Thumbprints);
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
