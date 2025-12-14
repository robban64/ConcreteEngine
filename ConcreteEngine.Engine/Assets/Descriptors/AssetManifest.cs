#region

using System.Text.Json.Serialization;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Engine.Assets.Descriptors;

internal sealed class AssetManifest(
    AssetResourceLayout resourceLayout,
    string? version)
{
    public AssetResourceLayout ResourceLayout { get; init; } = resourceLayout;
    public string? Version { get; init; } = version;
}

internal sealed class AssetResourceLayout(
    string shader,
    string texture,
    string mesh,
    string material,
    string? cubeMaps = null)
{
    public string Shader { get; } = shader;
    public string Texture { get; } = texture;
    public string Mesh { get; } = mesh;
    public string Material { get; } = material;

    [JsonPropertyName("cubemaps")] public string? CubeMaps { get; } = cubeMaps;
}

internal sealed class ShaderManifest : IAssetCatalog
{
    public required ShaderDescriptor[] Records { get; init; }
    public int Count => Records.Length;

    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

internal sealed class ShaderDescriptor(
    string name,
    string vertexFilename,
    string fragmentFilename,
    AssetLoadingMode loadMode = AssetLoadingMode.Processed
) : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.Shader;
    public string Name { get; } = name;
    public string VertexFilename { get; } = vertexFilename;
    public string FragmentFilename { get; } = fragmentFilename;
    public AssetLoadingMode LoadMode { get; } = loadMode;
}

internal sealed class TextureManifest : IAssetCatalog
{
    public required TextureDescriptor[] Records { get; init; }
    public int Count => Records.Length;

    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

internal sealed class TextureDescriptor(
    string name,
    string filename,
    TexturePreset preset = TexturePreset.LinearClamp,
    TexturePixelFormat pixelFormat = TexturePixelFormat.SrgbAlpha,
    TextureAnisotropy anisotropy = TextureAnisotropy.Off,
    float lodBias = 0,
    bool inMemory = false,
    AssetLoadingMode loadMode = AssetLoadingMode.Processed)
    : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.Texture2D;
    public string Name { get; } = name;
    public string Filename { get; } = filename;
    public TexturePreset Preset { get; } = preset;
    public TexturePixelFormat PixelFormat { get; } = pixelFormat;
    public TextureAnisotropy Anisotropy { get; } = anisotropy;
    public float LodBias { get; } = lodBias;
    public bool InMemory { get; } = inMemory;
    public AssetLoadingMode LoadMode { get; } = loadMode;
}

internal sealed class CubeMapManifest : IAssetCatalog
{
    public required CubeMapDescriptor[] Records { get; init; }
    public int Count => Records.Length;
    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

internal sealed class CubeMapDescriptor(
    string name,
    string[] textures,
    int width,
    int height,
    TexturePreset preset,
    TexturePixelFormat pixelFormat = TexturePixelFormat.Rgba,
    AssetLoadingMode loadMode = AssetLoadingMode.Processed
) : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.TextureCubeMap;
    public string Name { get; } = name;
    public string[] Textures { get; } = textures;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public TexturePreset Preset { get; } = preset;
    public TexturePixelFormat PixelFormat { get; } = pixelFormat;
    public AssetLoadingMode LoadMode { get; } = loadMode;
}

internal sealed class MeshManifest : IAssetCatalog
{
    public required MeshDescriptor[] Records { get; init; }
    public int Count => Records.Length;
    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

internal sealed class MeshDescriptor(
    string name,
    string filename,
    AssetLoadingMode loadMode = AssetLoadingMode.Processed)
    : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.Model;
    public string Name { get; } = name;
    public string Filename { get; } = filename;
    public AssetLoadingMode LoadMode { get; } = loadMode;
}