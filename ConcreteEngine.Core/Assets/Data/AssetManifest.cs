using System.Numerics;
using System.Text.Json.Serialization;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Core.Assets.Data;

public interface IAssetManifestRecord
{
    string Name { get; }
    AssetKind Kind { get; }
}

public sealed record AssetManifest(
    AssetResourceLayout ResourceLayout, 
    string? Version);

public sealed record AssetResourceLayout(
    string Shader,
    string Texture,
    string Mesh,
    string Material,
    [property: JsonPropertyName("cubemaps")]
    string? CubeMaps = null);

public sealed class ShaderManifest
{
    public required ShaderManifestRecord[] Records { get; init; }
}

public sealed record ShaderManifestRecord(
    string Name,
    string VertexFilename,
    string FragmentFilename
) : IAssetManifestRecord
{
    public AssetKind Kind => AssetKind.Shader;
}

public sealed class TextureManifest
{
    public required TextureManifestRecord[] Records { get; init; }
}

public sealed record TextureManifestRecord(
    string Name,
    string Filename,
    TexturePreset Preset = TexturePreset.LinearClamp,
    TexturePixelFormat PixelFormat = TexturePixelFormat.SrgbAlpha,
    TextureAnisotropy Anisotropy = TextureAnisotropy.Off,
    bool InMemory = false,
    float LodBias = 0)
    : IAssetManifestRecord
{
    public AssetKind Kind => AssetKind.Texture2D;
}

public sealed class CubeMapManifest
{
    public required CubeMapManifestRecord[] Records { get; init; }
}

public sealed record CubeMapManifestRecord(
    string Name,
    string[] Textures,
    int Width,
    int Height,
    TexturePreset Preset,
    TexturePixelFormat PixelFormat = TexturePixelFormat.Rgba
) : IAssetManifestRecord
{
    public AssetKind Kind => AssetKind.TextureCubeMap;
}

public sealed class MeshManifest
{
    public required MeshManifestRecord[] Records { get; init; }
}

public sealed record MeshManifestRecord(
    string Name,
    string Filename)
    : IAssetManifestRecord
{
    public AssetKind Kind => AssetKind.Mesh;
}

public sealed class MaterialManifest
{
    public required MaterialManifestRecord[] Records { get; init; }
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
    public AssetKind Kind => AssetKind.Material;
}