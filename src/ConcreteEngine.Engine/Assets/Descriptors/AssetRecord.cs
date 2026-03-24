using System.Text.Json.Serialization;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
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

    [JsonIgnore]
    public abstract AssetKind Kind { get; }

    public virtual AssetLoadingMode LoadMode { get; } = AssetLoadingMode.Processed;

    public static string GetDefaultFilename(AssetRecord record) => record.Files.First().Value;
}

internal sealed class ShaderRecord : AssetRecord
{
    public const string VertexFileKey = "Vertex";
    public const string FragmentFileKey = "Fragment";

    [JsonIgnore]
    public override AssetKind Kind => AssetKind.Shader;

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
    public AnisotropyLevel Anisotropy { get; init; } = AnisotropyLevel.Off;

    [JsonIgnore]
    public override AssetKind Kind => AssetKind.Texture;

    public static TextureRecord Create(string filename, string relativePath)
    {
        return new TextureRecord
        {
            GId = Guid.NewGuid(),
            Name = Path.GetFileNameWithoutExtension(filename),
            Files = { { "Source", relativePath } }
        };
    }
}

internal sealed class ModelRecord : AssetRecord
{
    public int SubMeshCount { get; init; }
    public bool HasAnimation { get; init; }

    [JsonIgnore]
    public override AssetKind Kind => AssetKind.Model;

    public static ModelRecord Create(string filename, string relativePath)
    {
        return new ModelRecord
        {
            GId = Guid.NewGuid(),
            Name = Path.GetFileNameWithoutExtension(filename),
            Files = { { "Source", relativePath } }
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

    public MaterialParamsRecord Parameters { get; init; }
    public TextureSlot[] TextureSlots { get; init; } = [];

    [JsonIgnore]
    public override AssetKind Kind => AssetKind.Material;

    public static MaterialRecord Create(string binPath)
    {
        return new MaterialRecord
        {
            GId = Guid.NewGuid(), Files = { { "Source", binPath } }, Profile = MaterialProfile.StaticModel,
        };
    }

    public sealed class TextureSlot
    {
        public string Name { get; init; }
        public int Slot { get; init; }

        [JsonPropertyName("slotKind")]
        public TextureUsage SlotKind { get; init; }

        [JsonPropertyName("textureKind")]
        public TextureKind TextureKind { get; init; } = TextureKind.Texture2D;

        public bool Srgb { get; init; } = true;
    }
}