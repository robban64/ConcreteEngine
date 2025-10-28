#region

using System.Diagnostics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

internal sealed class MaterialLoader
{
    private sealed record MatProfileInfo(string Shader, params ProfileSlot[] Slots);
    private readonly record struct ProfileSlot(TextureSlotKind SlotKind, TextureKind TexKind = TextureKind.Texture2D);


    private readonly Dictionary<MaterialProfile, MatProfileInfo> _profiles;

    internal MaterialLoader()
    {
        _profiles = new Dictionary<MaterialProfile, MatProfileInfo>()
        {
            [MaterialProfile.StaticModel] = new("Model",
                new ProfileSlot(TextureSlotKind.Albedo),
                new ProfileSlot(TextureSlotKind.Normal),
                new ProfileSlot(TextureSlotKind.Mask),
                new ProfileSlot(TextureSlotKind.Shadowmap)
            ),
            [MaterialProfile.Sky] = new("Skybox", new ProfileSlot(TextureSlotKind.Albedo, TextureKind.CubeMap)),
            [MaterialProfile.Terrain] = new("Terrain",
                new ProfileSlot(TextureSlotKind.Environment),
                new ProfileSlot(TextureSlotKind.Environment),
                new ProfileSlot(TextureSlotKind.Environment),
                new ProfileSlot(TextureSlotKind.Environment),
                new ProfileSlot(TextureSlotKind.Splatmap),
                new ProfileSlot(TextureSlotKind.Shadowmap)
            )
        };
    }

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
        var slots = Array.Empty<AssetTextureSlot>();

        string? shaderName = null;

        if (record.TextureSlots.Length > 0)
            slots = CreateSlots(record, store);
        else if (record.Profile != MaterialProfile.None)
        {
            var profile = _profiles[record.Profile];
            shaderName = profile.Shader;
            slots = CreateSlotsFromProfile(profile.Slots, record, store);
        }

        if (record.Shader != null) shaderName = record.Shader;

        if (string.IsNullOrEmpty(shaderName))
            throw new InvalidOperationException($"Missing shader name for material {record.Name}");

        var shader = store.GetByName<Shader>(shaderName).RefId;

        var matParams = new MaterialState(record.Parameters);
        return new MaterialTemplate(slots)
        {
            RawId = assetId,
            Name = record.Name,
            ShaderRef = shader,
            Params = matParams,
            IsCoreAsset = false
        };
    }

    private AssetTextureSlot[] CreateSlots(MaterialDescriptor record, IAssetStore store)
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
                throw new InvalidOperationException($"Texture {slot.Name} does not exists for {record.Name}");

            slotInfo[i] = new AssetTextureSlot(slotAssetId, slot.SlotKind, slot.TextureKind);
        }

        return slotInfo;
    }

    private AssetTextureSlot[] CreateSlotsFromProfile(ProfileSlot[] slotProfile, MaterialDescriptor record,
        IAssetStore store)
    {
        ArgumentNullException.ThrowIfNull(slotProfile, nameof(slotProfile));
        var slots = new List<AssetTextureSlot>();

        for (int i = 0; i < slotProfile.Length; i++)
        {
            var info = slotProfile[i];
            var name = record.ProfileSlots.Length > i ? record.ProfileSlots[i] : null;
            if (name == null)
            {
                slots.Add(new AssetTextureSlot(new AssetId(0), info.SlotKind, info.TexKind));
                continue;
            }

            switch (info.TexKind)
            {
                case TextureKind.Texture2D when store.TryGetByName<Texture2D>(name, out var tex):
                    slots.Add(new AssetTextureSlot(tex!.RawId, info.SlotKind, info.TexKind));
                    break;
                case TextureKind.CubeMap when store.TryGetByName<CubeMap>(name, out var cube):
                    slots.Add(new AssetTextureSlot(cube!.RawId, info.SlotKind, info.TexKind));
                    break;
                default:
                    slots.Add(new AssetTextureSlot(new AssetId(0), info.SlotKind, info.TexKind));
                    break;
            }
        }

        return slots.ToArray();
    }

}