namespace ConcreteEngine.Core.Configuration;

public sealed record AssetManagerConfiguration
{
    public string AssetPath { get; init; } = "assets";
    public string ManifestFilename { get; init; } = "manifest.json";
}