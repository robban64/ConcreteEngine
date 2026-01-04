using System.Text.Json.Serialization;
using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Descriptors;

internal sealed class AssetManifest
{
    public required AssetResourceLayout ResourceLayout { get; init; }
    public string? Version { get; init; }
}



internal sealed class AssetResourceLayout
{
    public required string Shader { get; init; }
    public required string Texture { get; init; }
    public required string Mesh { get; init; }
    public required string Material { get; init; }

    public string? CubeMaps { get; init; }
}

internal sealed class ShaderManifest : IAssetCatalog
{
    public required ShaderDescriptor[] Records { get; init; }
    public int Count => Records.Length;

    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

internal sealed class ShaderDescriptor : IAssetDescriptor
{
    public required string Name { get; init; }
    public required string VertexFilename { get; init; }
    public required string FragmentFilename { get; init; }

    public AssetKind Kind => AssetKind.Shader;
    public AssetLoadingMode LoadMode { get; } = AssetLoadingMode.Processed;
}

internal sealed class TextureManifest : IAssetCatalog
{
    public required TextureDescriptor[] Records { get; init; }
    public int Count => Records.Length;

    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

internal sealed class TextureDescriptor : IAssetDescriptor
{
    public required string Name { get; init; }
    public string? Filename { get; init; } = null;
    public string[]? MultiFilenames { get; init; } = null;
    public TexturePreset Preset { get; init; } = TexturePreset.LinearClamp;
    public TexturePixelFormat PixelFormat { get; init; } = TexturePixelFormat.SrgbAlpha;
    public TextureAnisotropyProfile Anisotropy { get; init; } = TextureAnisotropyProfile.Off;
    public float LodBias { get; init; }
    public bool InMemory { get; init; }

    public AssetKind Kind => AssetKind.Texture;
    public AssetLoadingMode LoadMode { get; init; } = AssetLoadingMode.Processed;
}


internal sealed class MeshManifest : IAssetCatalog
{
    public required MeshDescriptor[] Records { get; init; }
    public int Count => Records.Length;
    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

internal sealed class MeshDescriptor : IAssetDescriptor
{
    public required string Name { get; init; }
    public required string Filename { get; init; }
    public AssetLoadingMode LoadMode { get; } = AssetLoadingMode.Processed;
    public AssetKind Kind => AssetKind.Model;
}