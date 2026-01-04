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
[JsonDerivedType(typeof(ModelRecord), typeDiscriminator: nameof(AssetKind.Model))]
public abstract class AssetRecord
{
    public required Guid GId { get; init; }

    public string? Name { get; init; }

    public Dictionary<string, string> Files { get; init; } = new();

    public abstract AssetKind Kind { get; }

    public virtual AssetLoadingMode LoadMode { get; } = AssetLoadingMode.Processed;
}

internal sealed class ShaderRecord : AssetRecord
{
    public override AssetKind Kind => AssetKind.Shader;
    
    public static ShaderRecord Create(string vertPath, string fragPath)
    {
        return new ShaderRecord
        {
            GId = Guid.NewGuid(),
            Files = 
            { 
                { "Vertex", vertPath },
                { "Fragment", fragPath }
            }
        };
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
            Files = { { "Main", binPath } }
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

    public MaterialParamsDesc Parameters { get; init; } = new();
    public TextureSlot[] TextureSlots { get; init; } = [];


    public override AssetKind Kind => AssetKind.Material;

    public sealed class TextureSlot
    {
        public string Name { get; init; }
        public int Slot { get; init; }

        [JsonPropertyName("slotKind")] public TextureSlotKind SlotKind { get; init; }

        [JsonPropertyName("textureKind")] public TextureKind TextureKind { get; init; } = TextureKind.Texture2D;

        public bool Srgb { get; init; } = true;
    }

    public sealed class MaterialParamsDesc
    {
        public Color4? Color { get; init; }
        public float? Shininess { get; init; }
        public float? Specular { get; init; }
        public float? UvRepeat { get; init; }
    }
}