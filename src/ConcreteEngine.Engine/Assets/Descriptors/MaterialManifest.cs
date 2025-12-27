using System.Text.Json.Serialization;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Assets.Descriptors;

internal sealed class MaterialManifest : IAssetCatalog
{
    public required MaterialDescriptor[] Records { get; init; }
    public int Count => Records.Length;
    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

internal sealed class MaterialDescriptor : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.MaterialTemplate;

    public required string Name { get; init; }
    public string? Shader { get; init; }
    public AssetLoadingMode LoadMode { get; init; } = AssetLoadingMode.Processed;

    public bool DepthWrite { get; init; } = true;
    public bool ReceiveShadows { get; init; } = true;
    public bool CastShadows { get; init; } = true;

    public MaterialProfile Profile { get; init; } = MaterialProfile.None;
    public string?[] ProfileSlots { get; init; } = [];

    public MaterialParamsDesc Parameters { get; init; } = new();
    public TextureSlot[] TextureSlots { get; init; } = [];

    //public record struct TextureSlotBinds(bool AlphaMask = false, bool Normals = false, bool Shadows = false);

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