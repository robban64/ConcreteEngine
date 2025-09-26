#region

using System.Numerics;
using System.Text.Json.Serialization;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Assets.Manifest;

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
    public T[] Resources { get; init; } = Array.Empty<T>();
}

public interface IAssetManifestRecord
{
    string Name { get; }

    static abstract AssetKind Kind { get; }
}

public sealed record ShaderManifestRecord(
    string Name,
    string VertexFilename,
    string FragmentFilename
) : IAssetManifestRecord
{
    public static AssetKind Kind => AssetKind.Shader;
}

public sealed record TextureManifestRecord(
    string Name,
    string Filename,
    TexturePreset Preset = TexturePreset.LinearClamp,
    EnginePixelFormat PixelFormat = EnginePixelFormat.SrgbAlpha,
    TextureAnisotropy Anisotropy = TextureAnisotropy.Default,
    bool InMemory = false,
    float LodBias = -0.25f)
    : IAssetManifestRecord
{
    public static AssetKind Kind => AssetKind.Texture2D;
}

public sealed record MeshManifestRecord(
    string Name,
    string Filename)
    : IAssetManifestRecord
{
    public static AssetKind Kind => AssetKind.Mesh;
}

public sealed record CubeMapManifestRecord(
    string Name,
    string[] Textures,
    int Width,
    int Height,
    TexturePreset Preset,
    EnginePixelFormat PixelFormat = EnginePixelFormat.Rgba
) : IAssetManifestRecord
{
    public static AssetKind Kind => AssetKind.CubeMap;
}

public sealed record MaterialManifestRecord(
    string Name,
    string Shader,
    string[]? Textures,
    [property: JsonPropertyName("cubemap")]
    string? CubeMap
) : IAssetManifestRecord
{
    public Vector4 Color { get; init; } = Vector4.One;
    public static AssetKind Kind => AssetKind.Material;
}