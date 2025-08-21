#region

using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetManifest
{
    public required List<AssetShaderRecord> Shaders { get; init; } = [];
    public required List<AssetTextureRecord> Textures { get; init; } = [];
    public required List<AssetMaterialTemplate> Materials { get; init; } = [];
}

internal interface IAssetManifestRecord
{
    string Name { get; }
}

internal sealed record AssetShaderRecord(string Name, string VertShaderPath, string FragShaderPath, string[]? Samplers)
    : IAssetManifestRecord;

internal sealed record AssetTextureRecord(
    string Name,
    string Path,
    TexturePreset Preset,
    EnginePixelFormat PixelFormat = EnginePixelFormat.Rgba,
    float LodBias = -0.25f)
    : IAssetManifestRecord;

interface IAssetMaterialValue
{
    UniformValueKind Kind { get; }

    IMaterialValue ToMaterialValue();
}

public readonly struct AssetMaterialValue<T> : IAssetMaterialValue where T : struct, IEquatable<T>
{
    public T Value { get; init; }
    public UniformValueKind Kind { get; init; }
    public IMaterialValue ToMaterialValue() => ToMaterialValueInternal();
    private MaterialValue<T> ToMaterialValueInternal() => new(Value, Kind);
}

internal sealed record AssetMaterialTemplate(
    string Name,
    string Shader,
    string[]? Textures,
    Dictionary<ShaderUniform, IAssetMaterialValue>? Defaults
) : IAssetManifestRecord
{
    public Vector4 Color { get; init; } = Vector4.One;
}