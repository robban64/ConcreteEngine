#region

using System.Numerics;
using System.Text.Json.Serialization;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Assets;

public sealed class AssetManifest
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

public sealed class AssetResourceManifest<T> where T : IAssetManifestRecord
{
    public string? Folder { get; init; }
    public List<T> Resources { get; init; }
}

public interface IAssetManifestRecord
{
    string Name { get; }
}

public sealed record ShaderManifestRecord(
    string Name,
    string VertexFilename,
    string FragmentFilename
) : IAssetManifestRecord;

public sealed record TextureManifestRecord(
    string Name,
    string Filename,
    TexturePreset Preset,
    EnginePixelFormat PixelFormat = EnginePixelFormat.Rgba,
    TextureAnisotropy Anisotropy = TextureAnisotropy.Default,
    bool InMemory = false,
    float LodBias = -0.25f)
    : IAssetManifestRecord;

public sealed record MeshManifestRecord(
    string Name,
    string Filename)
    : IAssetManifestRecord;

public sealed record CubeMapManifestRecord(
    string Name,
    string[] Textures,
    int Width,
    int Height,
    TexturePreset Preset,
    EnginePixelFormat PixelFormat = EnginePixelFormat.Rgba
): IAssetManifestRecord;


public sealed record MaterialManifestRecord(
    string Name,
    string Shader,
    string[]? Textures,
    [property: JsonPropertyName("cubemap")]
    string? Cubemap
) : IAssetManifestRecord
{
    public Vector4 Color { get; init; } = Vector4.One;
}