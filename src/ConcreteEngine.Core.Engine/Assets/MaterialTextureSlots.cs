using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class MaterialTextureSlots
{
    public bool IsCubeMap { get;  }
    public bool HasNormalMap { get;  }
    public bool HasAlphaMap { get;  }
    public bool HasShadowMap { get;  }


    private readonly AssetTextureSlot[] _assetSlots;

    public MaterialTextureSlots(ReadOnlySpan<AssetTextureSlot> slots)
    {
        _assetSlots = slots.ToArray();
        
        IsCubeMap = false;
        HasNormalMap = false;
        HasShadowMap = false;

        foreach (var slot in _assetSlots)
        {
            if (!HasShadowMap) HasShadowMap = slot.SlotKind == MaterialSlotKind.Shadowmap;
            if (!slot.Asset.IsValid()) continue;
            if (!IsCubeMap) IsCubeMap = slot.TextureKind == TextureKind.CubeMap;
            if (!HasNormalMap) HasNormalMap = slot.SlotKind == MaterialSlotKind.Normal;
            if (!HasAlphaMap) HasAlphaMap = slot.SlotKind == MaterialSlotKind.Mask;
        }
    }

    public ReadOnlySpan<AssetTextureSlot> AssetSlots => _assetSlots;

    private void Refresh()
    {
    }
}