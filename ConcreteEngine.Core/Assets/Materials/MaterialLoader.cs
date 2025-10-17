#region

using System.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.Internal;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

internal sealed class MaterialLoader
{
    public List<MaterialTemplate>? LoadMaterials(AssetStore store, MaterialDescriptor[] descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        if (descriptors.Length == 0)
        {
            Debug.Assert(false);
            return null;
        }
        
        AssetAssembleDel<MaterialTemplate, MaterialDescriptor> factory = CreateTemplate;


        var result = new List<MaterialTemplate>();
        foreach (var record in descriptors)
        {
            var template = store.Register(record, factory);
            result.Add(template);
        }

        return result;
    }

    private MaterialTemplate CreateTemplate(AssetId assetId, MaterialDescriptor record, IAssetStore store)
    {
        var slotInfo = new AssetTextureSlot[record.TextureSlots.Length];
        for (int i = 0; i < slotInfo.Length; i++)
        {
            var slot = record.TextureSlots[i];
            AssetId? slotAsset = null;

            if (slot.SlotKind == TextureSlotKind.Shadowmap)
            {
                slotInfo[i] = new AssetTextureSlot(default, slot.SlotKind, slot.TextureKind);
                continue;
            }

            if (slot.TextureKind == TextureKind.Texture2D && store.TryGetByName<Texture2D>(slot.Name, out var tex))
                slotAsset = tex!.RawId;

            if (slot.TextureKind == TextureKind.CubeMap && store.TryGetByName<CubeMap>(slot.Name, out var cub))
                slotAsset = cub!.RawId;

            if (slotAsset is not { } slotAssetId)
                throw new InvalidOperationException($"Slot asset doesn't exist {slot}");
            
            slotInfo[i] = new AssetTextureSlot(slotAssetId, slot.SlotKind, slot.TextureKind);
        }

        var shader = store.GetByName<Shader>(record.Shader).RefId;

        var matParams = new MaterialState(record.Parameters);
        return new MaterialTemplate(slotInfo)
        {
            RawId = assetId,
            Name = record.Name,
            ShaderRef = shader,
            Params = matParams,
            IsCoreAsset = false,
            Generation = 0
        };
    }
}