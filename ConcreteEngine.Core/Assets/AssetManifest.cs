#region

using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetManifest
{
    public required List<AssetShaderEntry> Shaders { get; init; } = [];
    public required List<AssetTextureEntry> Textures { get; init; } = [];
}

internal record AssetManifestEntry
{
    public required string Name { get; init; }
    public required string Path { get; init; }
}

internal sealed record AssetShaderEntry : AssetManifestEntry;

internal sealed record AssetTextureEntry : AssetManifestEntry
{
    public EnginePixelFormat PixelFormat { get; init; } = EnginePixelFormat.Rgba;
}