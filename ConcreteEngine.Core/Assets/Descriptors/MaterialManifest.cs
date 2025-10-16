using System.Numerics;
using System.Text.Json.Serialization;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Core.Assets.Descriptors;

public sealed class MaterialManifest : IAssetCatalog
{
    public required MaterialDescriptor[] Records { get; init; }
    public int Count => Records.Length;
    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

public sealed record MaterialDescriptor( string Name, string Shader) : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.Material;
    public MaterialParamsDesc Parameters { get; init; } = new();
    public TextureSlot[] TextureSlots { get; init; } = Array.Empty<TextureSlot>();
    
    public sealed record TextureSlot(
        string Name,
        [property: JsonPropertyName("slotKind")]
        TextureSlotKind SlotKind,
        [property: JsonPropertyName("textureKind")]
        TextureKind TextureKind = TextureKind.Texture2D,
        bool Internal = true,
        bool Srgb = true);

    public sealed record MaterialParamsDesc(
        Color4? Color =null,
        float? Shininess=null,
        float? Specular=null,
        float? UvRepeat=null);
}