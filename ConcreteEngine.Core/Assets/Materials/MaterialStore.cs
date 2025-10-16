#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public delegate MaterialId MaterialProcessDel(
    ShaderId shaderId,
    in MaterialParams param,
    ReadOnlySpan<TextureSlotInfo> slots);

public delegate void MaterialUpdateDel(MaterialId materialId, in MaterialParams param);

public interface IMaterialStore
{
    Material CreateMaterial(string templateName, string name);
    MaterialProcessDel MaterialInvoke { get; set; }
}

public sealed class MaterialStore : IMaterialStore
{
    private readonly Dictionary<string, Material> _materials = new(8);

    private readonly AssetStore _assetStore;
    public IReadOnlyDictionary<string, Material> Materials => _materials;

    //TODO delete
    public MaterialProcessDel MaterialInvoke { get; set; } = null!;
    public MaterialUpdateDel MaterialUpdate { get; set; } = null!;

    internal MaterialStore(AssetStore assetStore)
    {
        _assetStore = assetStore;
    }


    internal void InitializeStore()
    {
        _assetStore.Process<MaterialTemplate>(Action);
        return;
        void Action(MaterialTemplate it) => CreateMaterialInternal(it, it.Name, false);
    }

    private Shader ResolveShader(Material material) => _assetStore.GetByRef(material.ShaderRef);

    public Material CreateMaterial(string templateName, string name)
    {
        var template = _assetStore.GetByName<MaterialTemplate>(templateName);
        return CreateMaterialInternal(template, name, true);
    }

    private Material CreateMaterialInternal(MaterialTemplate template, string name, bool upload)
    {
        var mat = new Material(template, name);
        _materials.Add(name, mat);
        if (upload) ProcessMaterial(mat);
        return mat;
    }


    //public void RemoveMaterial(Material material) => _materials.Remove(material);

    public Material Get(string name) => _materials[name];

    private void ProcessMaterial(Material material)
    {
        Span<TextureSlotInfo> textureSlots = stackalloc TextureSlotInfo[RenderLimits.TextureSlots];
        var count = CreateTextureSlotInfo(material, textureSlots);
        var shaderId = ResolveShader(material).ResourceId;
        var slots = textureSlots.Slice(0, count);
        var materialId = MaterialInvoke(shaderId, material.Parameters.GetDataParams(), slots);
        material.Attach(materialId);
    }


    public void ProcessStore()
    {
        Span<TextureSlotInfo> textureSlots = stackalloc TextureSlotInfo[RenderLimits.TextureSlots];
        foreach (var material in _materials.Values)
        {
            var count = CreateTextureSlotInfo(material, textureSlots);
            var shaderId = ResolveShader(material).ResourceId;
            var slots = textureSlots.Slice(0, count);
            var materialId = MaterialInvoke(shaderId, material.Parameters.GetDataParams(), slots);
            material.Attach(materialId);
        }
    }

    public void InvokeUpdateRenderMaterials()
    {
        foreach (var material in _materials.Values)
        {
            if (material.Id > 0) MaterialUpdate(material.Id, material.Parameters.GetDataParams());
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
        if (assetSlot.SlotKind == TextureSlotKind.Shadowmap) return default;
        
        if (assetSlot.TextureKind == TextureKind.Texture2D)
            return _assetStore.GetByRef(new AssetRef<Texture2D>(assetSlot.Asset)).ResourceId;

        if (assetSlot.TextureKind == TextureKind.CubeMap)
            return _assetStore.GetByRef(new AssetRef<CubeMap>(assetSlot.Asset)).ResourceId;

        throw new InvalidOperationException(nameof(assetSlot));
    }
}