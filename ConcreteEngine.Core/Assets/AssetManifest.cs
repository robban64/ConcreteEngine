#region

using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetManifest
{
    public required AssetResourceManifestDesc ResourceManifest { get; init; }

    public record AssetResourceManifestDesc(string Material, string Shader, string Texture);
}

internal sealed class AssetResourceManifest<T> where T : IAssetManifestRecord
{
    public string? Folder { get; init; }
    public List<T> Resources { get; init; }
}

internal interface IAssetManifestRecord
{
    string Name { get; }
}

internal sealed record AssetShaderRecord(
    string Name,
    string VertexFilename,
    string FragmentFilename,
    string[]? Samplers)
    : IAssetManifestRecord;

internal sealed record AssetTextureRecord(
    string Name,
    string Filename,
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