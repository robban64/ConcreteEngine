#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class MaterialTextureSlots
{
    public bool IsCubeMap { get; private set; }

    public bool HasNormalMap { get; private set; }
    public bool HasAlphaMap { get; set; } = false;
    public bool HasShadowMap { get; private set; }

    private readonly AssetTextureSlot[] _assetSlots;
    internal TextureSlotInfo[] CacheSlots { get; set; } = Array.Empty<TextureSlotInfo>();

    public MaterialTextureSlots(ReadOnlySpan<AssetTextureSlot> slots)
    {
        _assetSlots = slots.ToArray();
        Refresh();
    }


    public ReadOnlySpan<AssetTextureSlot> AssetSlots => _assetSlots;

    private void Refresh()
    {
        IsCubeMap = false;
        HasNormalMap = false;
        HasShadowMap = false;

        foreach (var slot in _assetSlots)
        {
            if (!HasShadowMap) HasShadowMap = slot.SlotKind == TextureSlotKind.Shadowmap;
            if (!slot.Asset.IsValid) continue;
            if (!IsCubeMap) IsCubeMap = slot.TextureKind == TextureKind.CubeMap;
            if (!HasNormalMap) HasNormalMap = slot.SlotKind == TextureSlotKind.Normal;
            if (!HasAlphaMap) HasAlphaMap = slot.SlotKind == TextureSlotKind.Mask;
        }
    }
}