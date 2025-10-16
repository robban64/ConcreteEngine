using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Core.Assets.Data;

public readonly record struct AssetTextureSlot(
    AssetId Asset,
    TextureSlotKind SlotKind,
    TextureKind TextureKind
    //bool Srgb
);

public sealed class MaterialTextureSlots
{
    public bool IsCubeMap { get; private set; }

    public bool HasNormalMap { get; private set; }
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
        HasNormalMap  = false;
        HasShadowMap = false;

        foreach (var slot in _slots)
        {
            if (!IsCubeMap) IsCubeMap = slot.TextureKind == TextureKind.CubeMap;
            if (!HasNormalMap) HasNormalMap = slot.SlotKind == TextureSlotKind.Normal;
            if (!HasShadowMap) HasShadowMap = slot.SlotKind == TextureSlotKind.Shadowmap;
        }
    }
}