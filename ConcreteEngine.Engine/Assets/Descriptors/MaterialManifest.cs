#region

using System.Text.Json.Serialization;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Assets.Descriptors;

public sealed class MaterialManifest : IAssetCatalog
{
    public required MaterialDescriptor[] Records { get; init; }
    public int Count => Records.Length;
    IReadOnlyList<IAssetDescriptor> IAssetCatalog.Records => Records;
}

public sealed record MaterialDescriptor(
    string Name,
    string? Shader = null,
    AssetLoadingMode LoadMode = AssetLoadingMode.Processed) : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.Material;

    public bool DepthWrite { get; init; } = true;
    public bool ReceiveShadows { get; init; } = true;
    public bool CastShadows { get; init; } = true;

    public MaterialProfile Profile { get; init; } = MaterialProfile.None;
    public string?[] ProfileSlots { get; init; } = Array.Empty<string?>();

    public MaterialParamsDesc Parameters { get; init; } = new();
    public TextureSlot[] TextureSlots { get; init; } = Array.Empty<TextureSlot>();

    public sealed record TextureSlotBinds(bool AlphaMask = false, bool Normals = false, bool Shadows = false);

    public sealed record TextureSlot(
        string Name,
        int Slot,
        [property: JsonPropertyName("slotKind")]
        TextureSlotKind SlotKind,
        [property: JsonPropertyName("textureKind")]
        TextureKind TextureKind = TextureKind.Texture2D,
        bool Internal = true,
        bool Srgb = true);

    public sealed record MaterialParamsDesc(
        Color4? Color = null,
        float? Shininess = null,
        float? Specular = null,
        float? UvRepeat = null);
}