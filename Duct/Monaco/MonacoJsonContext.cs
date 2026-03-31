using System.Text.Json.Serialization;

namespace Duct.Monaco;

/// <summary>
/// AOT-compatible JSON source generator context for Monaco editor serialization.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MonacoInitConfig))]
[JsonSerializable(typeof(string))]
internal partial class MonacoJsonContext : JsonSerializerContext;

/// <summary>
/// Typed configuration for initial Monaco editor setup, replacing Dictionary&lt;string, object&gt;.
/// </summary>
internal sealed class MonacoInitConfig
{
    public string Value { get; init; } = "";
    public string Language { get; init; } = "plaintext";
    public string Theme { get; init; } = "vs";
    public bool ReadOnly { get; init; }
    public double FontSize { get; init; } = 14.0;
    public bool WordWrap { get; init; }
    public bool Minimap { get; init; }
}
