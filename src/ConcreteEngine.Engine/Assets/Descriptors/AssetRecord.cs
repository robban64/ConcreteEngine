using System.Text.Json.Serialization;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Assets.Descriptors;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "assetKind")]
[JsonDerivedType(typeof(ShaderRecord), typeDiscriminator: nameof(AssetKind.Shader))]
[JsonDerivedType(typeof(TextureRecord), typeDiscriminator: nameof(AssetKind.Texture))]
[JsonDerivedType(typeof(ModelRecord), typeDiscriminator: nameof(AssetKind.Model))]
[JsonDerivedType(typeof(MaterialRecord), typeDiscriminator: nameof(AssetKind.Material))]
public abstract class AssetRecord
{
    public required Guid GId { get; init; }

    public string Name { get; init; } = null!;

    public Dictionary<string, string> Files { get; init; } = new();

    [JsonIgnore] public abstract AssetKind Kind { get; }

    public virtual AssetLoadingMode LoadMode { get; } = AssetLoadingMode.Processed;

    public static string GetDefaultFilename(AssetRecord record) => record.Files.First().Value;
}

internal sealed class ShaderRecord : AssetRecord
{
    public const string VertexFileKey = "Vertex";
    public const string FragmentFileKey = "Fragment";

    [JsonIgnore] public override AssetKind Kind => AssetKind.Shader;

    public static (string, string) SetFileNames(ShaderRecord record)
    {
        return (record.Files[VertexFileKey], record.Files[FragmentFileKey]);
    }

    public static (string, string) GetFilenames(ShaderRecord record)
    {
        return (record.Files[VertexFileKey], record.Files[FragmentFileKey]);
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

    [JsonIgnore] public override AssetKind Kind => AssetKind.Texture;

    public static TextureRecord Create(string relativePath)
    {
        return new TextureRecord { GId = Guid.NewGuid(), Files = { { "Source", relativePath } } };
    }
}

internal sealed class ModelRecord : AssetRecord
{
    public int SubMeshCount { get; init; }
    public bool HasAnimation { get; init; }

    [JsonIgnore] public override AssetKind Kind => AssetKind.Model;

    public static ModelRecord Create(string binPath)
    {
        return new ModelRecord { GId = Guid.NewGuid(), Files = { { "Source", binPath } } };
    }
}

internal sealed class MaterialRecord : AssetRecord
{
    public string? Shader { get; init; }

    public bool DepthWrite { get; init; } = true;
    public bool ReceiveShadows { get; init; } = true;
    public bool CastShadows { get; init; } = true;

    public MaterialTemplateProfile Profile { get; init; } = MaterialTemplateProfile.None;
    public string?[] ProfileSlots { get; init; } = [];

    public MaterialTemplateParams Parameters { get; init; } = new();
    public TextureSlot[] TextureSlots { get; init; } = [];

    [JsonIgnore] public override AssetKind Kind => AssetKind.Material;

    public static MaterialRecord Create(string binPath)
    {
        return new MaterialRecord
        {
            GId = Guid.NewGuid(), Files = { { "Source", binPath } }, Profile = MaterialTemplateProfile.StaticModel,
        };
    }

    public sealed class TextureSlot
    {
        public string Name { get; init; }
        public int Slot { get; init; }

        [JsonPropertyName("slotKind")] public MaterialSlotKind SlotKind { get; init; }

        [JsonPropertyName("textureKind")] public TextureKind TextureKind { get; init; } = TextureKind.Texture2D;

        public bool Srgb { get; init; } = true;
    }
}