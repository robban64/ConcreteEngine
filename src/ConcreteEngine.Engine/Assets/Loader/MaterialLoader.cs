using System.Diagnostics;
using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Assets.Materials;

internal sealed class MaterialLoader
{
    private sealed record MatProfileInfo(string Shader, params ProfileSlot[] Slots);

    private readonly record struct ProfileSlot(TextureSlotKind SlotKind, TextureKind TexKind = TextureKind.Texture2D);

    private readonly Dictionary<MaterialProfile, MatProfileInfo> _profiles;

    internal MaterialLoader()
    {
        _profiles = new Dictionary<MaterialProfile, MatProfileInfo>
        {
            [MaterialProfile.None] = new("Model"),
            [MaterialProfile.Particle] = new("Particle", new ProfileSlot(TextureSlotKind.Albedo)),
            [MaterialProfile.Sky] = new("Skybox", new ProfileSlot(TextureSlotKind.Albedo, TextureKind.CubeMap)),
            [MaterialProfile.StaticModel] = new("Model",
                new ProfileSlot(TextureSlotKind.Albedo),
                new ProfileSlot(TextureSlotKind.Normal),
                new ProfileSlot(TextureSlotKind.Mask),
                new ProfileSlot(TextureSlotKind.Shadowmap)
            ),
            [MaterialProfile.AnimatedModel] = new("ModelAnimated",
                new ProfileSlot(TextureSlotKind.Albedo),
                new ProfileSlot(TextureSlotKind.Normal),
                new ProfileSlot(TextureSlotKind.Mask),
                new ProfileSlot(TextureSlotKind.Shadowmap)
            ),
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

        LoadSimpleAssetDel<MaterialTemplate, MaterialDescriptor> factory = CreateTemplate;

        var result = new List<MaterialTemplate>();
        foreach (var record in descriptors)
        {
            var template = store.Register(record, factory);
            result.Add(template);
        }

        return result;
    }

    public MaterialTemplate CreateEmbeddedTemplate(AssetId asset, MaterialEmbeddedRecord desc, AssetStore store)
    {
        AssetTextureSlot[] slots =
        [
            new(default, TextureSlotKind.Albedo),
            new(default, TextureSlotKind.Normal),
            new(default, TextureSlotKind.Mask),
            new(default, TextureSlotKind.Shadowmap),
        ];
        foreach (var (key, gid) in desc.EmbeddedTextures)
        {
            var (materialIndex, textureIndex) = key;
            if (materialIndex != desc.Index) continue;

            if (!store.TryGetByGuid(gid, out Texture2D texture))
                throw new ArgumentException($"Embedded texture {textureIndex}  not found: {gid}");

            if (texture.SlotKind == TextureSlotKind.Albedo)
                slots[0] = slots[0].WithAssetId(texture.Id);

            if (texture.SlotKind == TextureSlotKind.Normal)
                slots[1] = slots[1].WithAssetId(texture.Id);
        }

        var shaderName = desc.IsAnimated ? "ModelAnimated" : "Model";

        var matParams = new MaterialState(in desc.Params);
        return new MaterialTemplate(slots)
        {
            Id = asset,
            Name = desc.AssetName,
            ShaderRef = store.GetByName<Shader>(shaderName).RefId,
            Params = matParams,
            IsCoreAsset = false
        };
    }


    private MaterialTemplate CreateTemplate(AssetId assetId, MaterialDescriptor record, AssetStore store)
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
            Id = assetId,
            Name = record.Name,
            ShaderRef = shader,
            Params = matParams,
            IsCoreAsset = false
        };
    }

    private AssetTextureSlot[] CreateSlots(MaterialDescriptor record, AssetStore store)
    {
        if (record.TextureSlots.Length == 0)
        {
            return [new AssetTextureSlot(default, TextureSlotKind.Albedo)];
        }

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
                slotAsset = tex!.Id;

            if (slot.TextureKind == TextureKind.CubeMap && store.TryGetByName<CubeMap>(slot.Name, out var cub))
                slotAsset = cub!.Id;

            if (slotAsset is not { } slotAssetId)
                throw new InvalidOperationException($"Texture {slot.Name} does not exists for {record.Name}");

            slotInfo[i] = new AssetTextureSlot(slotAssetId, slot.SlotKind, slot.TextureKind);
        }

        return slotInfo;
    }

    private AssetTextureSlot[] CreateSlotsFromProfile(ProfileSlot[] profile, MaterialDescriptor desc, AssetStore store)
    {
        ArgumentNullException.ThrowIfNull(profile, nameof(profile));
        var slots = new List<AssetTextureSlot>();

        for (int i = 0; i < profile.Length; i++)
        {
            var info = profile[i];
            var name = desc.ProfileSlots.Length > i ? desc.ProfileSlots[i] : null;
            if (name == null)
            {
                slots.Add(new AssetTextureSlot(new AssetId(0), info.SlotKind, info.TexKind));
                continue;
            }

            switch (info.TexKind)
            {
                case TextureKind.Texture2D when store.TryGetByName<Texture2D>(name, out var tex):
                    slots.Add(new AssetTextureSlot(tex!.Id, info.SlotKind, info.TexKind));
                    break;
                case TextureKind.CubeMap when store.TryGetByName<CubeMap>(name, out var cube):
                    slots.Add(new AssetTextureSlot(cube!.Id, info.SlotKind, info.TexKind));
                    break;
                default:
                    slots.Add(new AssetTextureSlot(new AssetId(0), info.SlotKind, info.TexKind));
                    break;
            }
        }

        return slots.ToArray();
    }
}