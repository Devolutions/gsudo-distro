using ThumbprintBundle.Sample;

var prototypeRoot = FindPrototypeRoot();

var bundlePath = Path.Combine(prototypeRoot, "bundle", "thumbprints.bundle.jwt");
var publicKeyPath = Path.Combine(prototypeRoot, "keys", "jwt-public.pem");
var certsRoot = Path.GetFullPath(Path.Combine(prototypeRoot, ".."));

var certPaths = new[]
{
    Path.Combine(certsRoot, "Devolutions_CodeSign_2023-2026.crt"),
    Path.Combine(certsRoot, "Devolutions_CodeSign_2025-2028.crt")
};

var jwt = File.ReadAllText(bundlePath).Trim();
var publicPem = File.ReadAllText(publicKeyPath);

var claims = BundleVerifier.VerifyToken(jwt, publicPem);
Console.WriteLine($"Bundle verified. Version={claims.Version}, Entries={claims.Thumbprints.Count}");

foreach (var certPath in certPaths)
{
    var allowed = BundleVerifier.IsCertificateAllowed(certPath, claims);
    var certFile = Path.GetFileName(certPath);
    Console.WriteLine($"  {certFile}: {(allowed ? "ALLOWED" : "BLOCKED")}");
}

return;

static string FindPrototypeRoot()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);

    while (current is not null)
    {
        var bundlePath = Path.Combine(current.FullName, "bundle", "thumbprints.bundle.jwt");
        var keyPath = Path.Combine(current.FullName, "keys", "jwt-public.pem");
        if (File.Exists(bundlePath) && File.Exists(keyPath))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate certs/prototype root from current execution directory.");
}
