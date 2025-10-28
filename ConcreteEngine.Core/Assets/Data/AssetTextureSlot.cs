#region

using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets.Data;

public readonly record struct AssetTextureSlot(
    AssetId Asset,
    TextureSlotKind SlotKind,
    TextureKind TextureKind = TextureKind.Texture2D
    //bool Srgb
);

public sealed class MaterialTextureSlots
{
    public bool IsCubeMap { get; private set; }
    
    public bool HasNormalMap { get; private set; }
    public bool HasAlphaMap { get; set; } = false;
    public bool HasShadowMap { get; private set; }

    private AssetTextureSlot[] _slots;

    public MaterialTextureSlots(ReadOnlySpan<AssetTextureSlot> slots)
    {
        _slots = slots.ToArray();
        Refresh();
    }

    public ReadOnlySpan<AssetTextureSlot> Slots => _slots;

    private void Refresh()
    {
        IsCubeMap = false;
        HasNormalMap = false;
        HasShadowMap = false;

        foreach (var slot in _slots)
        {
            if (!HasShadowMap) HasShadowMap = slot.SlotKind == TextureSlotKind.Shadowmap;
            if(!slot.Asset.IsValid) continue;
            if (!IsCubeMap) IsCubeMap = slot.TextureKind == TextureKind.CubeMap;
            if (!HasNormalMap) HasNormalMap = slot.SlotKind == TextureSlotKind.Normal;
            if (!HasAlphaMap) HasAlphaMap = slot.SlotKind == TextureSlotKind.Mask;
        }
    }
}