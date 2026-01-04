using System.Text.Json.Serialization;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Assets.Descriptors;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Kind")]
[JsonDerivedType(typeof(ShaderRecord), typeDiscriminator: nameof(AssetKind.Shader))]
[JsonDerivedType(typeof(TextureRecord), typeDiscriminator: nameof(AssetKind.Texture))]
[JsonDerivedType(typeof(ModelRecord), typeDiscriminator: nameof(AssetKind.Model))]
[JsonDerivedType(typeof(MaterialRecord), typeDiscriminator: nameof(AssetKind.Material))]
public abstract class AssetRecord
{
    public required Guid GId { get; init; }

    public string? Name { get; init; }

    public Dictionary<string, string> Files { get; init; } = new();

    public abstract AssetKind Kind { get; }

    public virtual AssetLoadingMode LoadMode { get; } = AssetLoadingMode.Processed;
    
    public static string GetDefaultFilename(AssetRecord record) => record.Files.First().Value;
}

internal sealed class ShaderRecord : AssetRecord
{
    public const string VertexFilename = "Vertex";
    public const string FragmentFilename = "Fragment";

    public override AssetKind Kind => AssetKind.Shader;

    public static (string, string) GetFilenames(ShaderRecord record)
    {
        return (record.Files[VertexFilename], record.Files[FragmentFilename]);
    }
}

internal sealed class TextureRecord : AssetRecord
{
    public float LodBias { get; init; }
    public bool InMemory { get; init; }

    public TexturePreset Preset { get; init; } = TexturePreset.LinearClamp;
    public TextureKind TextureKind { get; init; } = TextureKind.Texture2D;
    public TexturePixelFormat PixelFormat { get; init; } = TexturePixelFormat.SrgbAlpha;
    public TextureAnisotropyProfile Anisotropy { get; init; } = TextureAnisotropyProfile.Off;

    public override AssetKind Kind => AssetKind.Texture;
    
    public static TextureRecord Create(string relativePath)
    {
        return new TextureRecord
        {
            GId = Guid.NewGuid(),
            Files = { { "Source", relativePath } }
        };
    }
}

internal sealed class ModelRecord : AssetRecord
{
    public int SubMeshCount { get; init; }
    public bool HasAnimation { get; init; }

    public override AssetKind Kind => AssetKind.Model;
    
    public static ModelRecord Create(string binPath)
    {
        return new ModelRecord
        {
            GId = Guid.NewGuid(),
            Files = { { "Source", binPath } }
        };
    }
}

internal sealed class MaterialRecord : AssetRecord
{
    public string? Shader { get; init; }

    public bool DepthWrite { get; init; } = true;
    public bool ReceiveShadows { get; init; } = true;
    public bool CastShadows { get; init; } = true;

    public MaterialProfile Profile { get; init; } = MaterialProfile.None;
    public string?[] ProfileSlots { get; init; } = [];

    public MaterialDescriptor.MaterialParamsDesc Parameters { get; init; } = new();
    public MaterialDescriptor.TextureSlot[] TextureSlots { get; init; } = [];


    public override AssetKind Kind => AssetKind.Material;

    public static MaterialRecord Create(string binPath)
    {
        return new MaterialRecord
        {
            GId = Guid.NewGuid(),
            Files = { { "Source", binPath } },
            Profile = MaterialProfile.StaticModel,
        };
    }

}