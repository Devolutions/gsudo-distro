using System.Text.Json.Serialization;

namespace ThumbprintBundle.Sample;

public sealed class ThumbprintBundleClaims
{
    [JsonPropertyName("iss")]
    public string Issuer { get; set; } = string.Empty;

    [JsonPropertyName("aud")]
    public string Audience { get; set; } = string.Empty;

    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }

    [JsonPropertyName("nbf")]
    public long NotBefore { get; set; }

    [JsonPropertyName("exp")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("ver")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("thumbprints")]
    public List<ThumbprintEntry> Thumbprints { get; set; } = new();
}

public sealed class ThumbprintEntry
{
    [JsonPropertyName("x5t")]
    public string X5t { get; set; } = string.Empty;

    [JsonPropertyName("x5t#S256")]
    public string X5tS256 { get; set; } = string.Empty;
}
