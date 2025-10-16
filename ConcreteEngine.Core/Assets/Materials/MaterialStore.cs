#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public delegate MaterialId MaterialProcessDel(
    ShaderId shaderId,
    in MaterialParams param,
    ReadOnlySpan<TextureSlotInfo> slots);


public interface IMaterialStore
{
    Material CreateMaterial(string name, string templateName);


    Material CreateMaterial(string name, MaterialTemplate template);
}

public sealed class MaterialStore : IMaterialStore
{
    private readonly Dictionary<string, Material> _materials = new(8);

    private readonly AssetStore _assetStore;

    internal MaterialStore(AssetStore assetStore)
    {
        _assetStore = assetStore;
    }

    public IReadOnlyDictionary<string, Material> Materials => _materials;

    internal void InitializeStore()
    {
        _assetStore.Process<MaterialTemplate>(Action);
        return;
        void Action(MaterialTemplate it) => CreateMaterial(it.Name, it);
    }

    public Material CreateMaterial(string name, string templateName)
    {
        var template = _assetStore.GetByName<MaterialTemplate>(templateName);
        return CreateMaterial(name, template);
    }
    
    public Material CreateMaterial(string name, MaterialTemplate template)
    {
        var mat = new Material(template);
        _materials.Add(template.Name, mat);
        return mat;
    }
    


    //public void RemoveMaterial(Material material) => _materials.Remove(material);

    public Material Get(string name) => _materials[name];

    public void ProcessStore(MaterialProcessDel action)
    {
        Span<TextureSlotInfo> textureSlots = stackalloc TextureSlotInfo[RenderLimits.TextureSlots];
        
        foreach (var material in _materials.Values)
        {
            var count = CreateTextureSlotInfo(material, textureSlots);
            action(material.ShaderId, material.Parameters.GetDataParams(), textureSlots.Slice(0,count));
        }
    }

    private int CreateTextureSlotInfo(Material material, Span<TextureSlotInfo> span)
    {
        for (var i = 0; i < material.TextureSlots.Slots.Length; i++)
        {
            var slot = material.TextureSlots.Slots[i];
            var textureId = GetTextureId(slot);
            span[i] = new TextureSlotInfo(textureId, slot.SlotKind, slot.TextureKind);
        }

        return material.TextureSlots.Slots.Length;
    }

    private TextureId GetTextureId(AssetTextureSlot assetSlot)
    {

        if (assetSlot.TextureKind == TextureKind.Texture2D)
            return _assetStore.GetByRef(new AssetRef<Texture2D>(assetSlot.Asset)).ResourceId;

        if (assetSlot.TextureKind == TextureKind.CubeMap)
            return _assetStore.GetByRef(new AssetRef<CubeMap>(assetSlot.Asset)).ResourceId;

        throw new InvalidOperationException(nameof(assetSlot));

    }
}