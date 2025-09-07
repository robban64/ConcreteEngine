#region

using System.Numerics;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetManifest
{
    public required AssetResourceManifestDesc ResourceManifest { get; init; }

    public record AssetResourceManifestDesc(
        string Material,
        string Shader,
        string Texture,
        string Mesh,
        [property: JsonPropertyName("cubemaps")]
        string? CubeMaps = null);
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
    string FragmentFilename
) : IAssetManifestRecord;

internal sealed record AssetTextureRecord(
    string Name,
    string Filename,
    TexturePreset Preset,
    EnginePixelFormat PixelFormat = EnginePixelFormat.Rgba,
    TextureAnisotropy Anisotropy = TextureAnisotropy.Default,
    bool InMemory = false,
    float LodBias = -0.25f)
    : IAssetManifestRecord;

internal sealed record AssetMeshRecord(
    string Name,
    string Filename)
    : IAssetManifestRecord;

internal sealed record AssetCubeMapRecord(
    string Name,
    string[] Textures,
    int Width,
    int Height,
    TexturePreset Preset,
    EnginePixelFormat PixelFormat = EnginePixelFormat.Rgba
)
    : IAssetManifestRecord;

interface IAssetMaterialValue
{
    UniformValueKind Kind { get; }

    IMaterialValue ToMaterialValue();
}


internal sealed record AssetMaterialTemplate(
    string Name,
    string Shader,
    string[]? Textures
) : IAssetManifestRecord
{
    public Vector4 Color { get; init; } = Vector4.One;
}