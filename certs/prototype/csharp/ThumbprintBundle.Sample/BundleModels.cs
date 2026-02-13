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
    public List<string> Thumbprints { get; set; } = new();
}
