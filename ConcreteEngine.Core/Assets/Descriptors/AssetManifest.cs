#region

using System.Numerics;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets.Descriptors;

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

public sealed class ShaderManifest : IAssetCatalog
{
    public required ShaderDescriptor[] Records { get; init; }
    public int Count => Records.Length;
    
    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

public sealed record ShaderDescriptor(
    string Name,
    string VertexFilename,
    string FragmentFilename
) : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.Shader;
}

public sealed class TextureManifest : IAssetCatalog
{
    public required TextureDescriptor[] Records { get; init; }
    public int Count => Records.Length;
    
    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

public sealed record TextureDescriptor(
    string Name,
    string Filename,
    TexturePreset Preset = TexturePreset.LinearClamp,
    TexturePixelFormat PixelFormat = TexturePixelFormat.SrgbAlpha,
    TextureAnisotropy Anisotropy = TextureAnisotropy.Off,
    bool InMemory = false,
    float LodBias = 0)
    : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.Texture2D;
}

public sealed class CubeMapManifest : IAssetCatalog
{
    public required CubeMapDescriptor[] Records { get; init; }
    public int Count => Records.Length;
    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;

}

public sealed record CubeMapDescriptor(
    string Name,
    string[] Textures,
    int Width,
    int Height,
    TexturePreset Preset,
    TexturePixelFormat PixelFormat = TexturePixelFormat.Rgba
) : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.TextureCubeMap;
}

public sealed class MeshManifest : IAssetCatalog
{
    public required MeshDescriptor[] Records { get; init; }
    public int Count => Records.Length;
    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;

}

public sealed record MeshDescriptor(
    string Name,
    string Filename)
    : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.Mesh;
}
