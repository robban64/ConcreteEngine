#region

using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetManifest
{
    public required List<AssetShaderRecord> Shaders { get; init; } = [];
    public required List<AssetTextureRecord> Textures { get; init; } = [];
}

internal record AssetManifestRecord
{
    public required string Name { get; init; }
    public required string Path { get; init; }
}

internal sealed record AssetShaderRecord : AssetManifestRecord
{
    public required string[] Samplers { get; init; }
}

internal sealed record AssetTextureRecord : AssetManifestRecord
{
    public EnginePixelFormat PixelFormat { get; init; } = EnginePixelFormat.Rgba;
    public TexturePreset Preset { get; init; } = TexturePreset.LinearClamp;
}