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

public sealed record MaterialDescriptor(
    string Name,
    string Shader,
    MaterialDescriptor.TextureSlot[] TextureSlots,
    MaterialDescriptor.MaterialParamsDesc Parameters
) : IAssetDescriptor
{
    public AssetKind Kind => AssetKind.Material;

    public sealed record TextureSlot(
        string Name,
        TextureSlotKind SlotKind,
        TextureKind TextureKind,
        bool Internal = true,
        bool Srgb = true);

    public sealed record MaterialParamsDesc(
        Color4? Color,
        float? Shininess,
        float? Specular,
        float? UvRepeat);
}