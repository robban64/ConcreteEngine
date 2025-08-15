#region

using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetManifest
{
    public required List<AssetShaderRecord> Shaders { get; init; } = [];
    public required List<AssetTextureRecord> Textures { get; init; } = [];
}

internal interface IAssetManifestRecord
{
    string Name { get; }
}

internal sealed record AssetShaderRecord(string Name, string VertShaderPath, string FragShaderPath, string[] Samplers)
    : IAssetManifestRecord;

internal sealed record AssetTextureRecord(
    string Name,
    string Path,
    TexturePreset Preset,
    EnginePixelFormat PixelFormat = EnginePixelFormat.Rgba)
    : IAssetManifestRecord;