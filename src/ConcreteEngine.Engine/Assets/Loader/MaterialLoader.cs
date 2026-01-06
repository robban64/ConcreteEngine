using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class MaterialLoader : AssetTypeLoader<MaterialTemplate, MaterialRecord>
{
    //
    private sealed record MatProfileInfo(string Shader, params ProfileSlot[] Slots);
    private readonly record struct ProfileSlot(MaterialSlotKind SlotKind, TextureKind TexKind = TextureKind.Texture2D);
    //

    private Dictionary<MaterialTemplateProfile, MatProfileInfo> _profiles;

    private AssetStore _store;

    internal MaterialLoader(AssetStore store, AssetGfxUploader gfxUploader) : base(gfxUploader)
    {
        _store = store;

    }

    public override void Setup()
    {
        _profiles = CreateSlotProfiles();
        IsActive = true;
    }

    public override void Teardown()
    {
        _profiles.Clear();
        _profiles = null!;
        _store = null!;
        IsActive = false;
    }

    protected override MaterialTemplate Load(MaterialRecord record, ref LoaderContext ctx)
    {
        var slots = Array.Empty<AssetTextureSlot>();

        string? shaderName = null;

        if (record.TextureSlots.Length > 0)
            slots = CreateSlots(record);
        else if (record.Profile != MaterialTemplateProfile.None)
        {
            var profile = _profiles[record.Profile];
            shaderName = profile.Shader;
            slots = CreateSlotsFromProfile(profile.Slots, record);
        }

        if (record.Shader != null) shaderName = record.Shader;

        if (string.IsNullOrEmpty(shaderName))
            throw new InvalidOperationException($"Missing shader name for material {record.Name}");

        var shader = _store.GetByName<Shader>(shaderName).Id;

        return new MaterialTemplate(slots)
        {
            Id = ctx.Id,
            GId = record.GId,
            Name = record.Name,
            AssetShader = shader,
            Params = record.Parameters
        };
    }

    public MaterialTemplate LoadEmbedded(AssetId assetId, MaterialEmbeddedRecord embedded)
    {
        AssetTextureSlot[] slots =
        [
            new(default, MaterialSlotKind.Albedo),
            new(default, MaterialSlotKind.Normal),
            new(default, MaterialSlotKind.Mask),
            new(default, MaterialSlotKind.Shadowmap),
        ];
        foreach (var (key, gid) in embedded.EmbeddedTextures)
        {
            var (materialIndex, textureIndex) = key;
            if (materialIndex != embedded.Index) continue;

            if (!_store.TryGetByGuid(gid, out Texture2D texture))
                throw new ArgumentException($"Embedded texture {textureIndex}  not found: {gid}");

            if (texture.SlotKind == MaterialSlotKind.Albedo)
                slots[0] = slots[0].WithAssetId(texture.Id);

            if (texture.SlotKind == MaterialSlotKind.Normal)
                slots[1] = slots[1].WithAssetId(texture.Id);
        }

        var shaderName = embedded.IsAnimated ? "ModelAnimated" : "Model";

        var matParams = new MaterialState(in embedded.Data, embedded.Props);
        return new MaterialTemplate(slots)
        {
            Id = assetId,
            GId = embedded.GId,
            Name = embedded.AssetName,
            AssetShader = _store.GetByName<Shader>(shaderName).Id,
            Params = new MaterialTemplateParams()
        };
    }

    private AssetTextureSlot[] CreateSlots(MaterialRecord embedded)
    {
        if (embedded.TextureSlots.Length == 0)
        {
            return [new AssetTextureSlot(default, MaterialSlotKind.Albedo)];
        }

        var slotInfo = new AssetTextureSlot[embedded.TextureSlots.Length];
        for (int i = 0; i < slotInfo.Length; i++)
        {
            var slot = embedded.TextureSlots[i];
            AssetId? slotAsset = null;

            if (slot.SlotKind == MaterialSlotKind.Shadowmap)
            {
                slotInfo[i] = new AssetTextureSlot(default, slot.SlotKind, slot.TextureKind);
                continue;
            }

            if (_store.TryGetByName<Texture2D>(slot.Name, out var tex))
                slotAsset = tex!.Id;

            if (slotAsset is not { } slotAssetId)
                throw new InvalidOperationException($"Texture {slot.Name} does not exists for {embedded.Name}");

            slotInfo[i] = new AssetTextureSlot(slotAssetId, slot.SlotKind, slot.TextureKind);
        }

        return slotInfo;
    }

    private AssetTextureSlot[] CreateSlotsFromProfile(ProfileSlot[] profile, MaterialRecord desc)
    {
        ArgumentNullException.ThrowIfNull(profile);
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

            var tex = _store.GetByName<Texture2D>(name);
            slots.Add(new AssetTextureSlot(tex!.Id, info.SlotKind, info.TexKind));
        }

        return slots.ToArray();
    }
    
    private static Dictionary<MaterialTemplateProfile, MatProfileInfo> CreateSlotProfiles() => new()
    {
        [MaterialTemplateProfile.None] = new("Model"),
        [MaterialTemplateProfile.Particle] = new("Particle", new ProfileSlot(MaterialSlotKind.Albedo)),
        [MaterialTemplateProfile.Sky] = new("Skybox", new ProfileSlot(MaterialSlotKind.Albedo, TextureKind.CubeMap)),
        [MaterialTemplateProfile.StaticModel] = new("Model",
            new ProfileSlot(MaterialSlotKind.Albedo),
            new ProfileSlot(MaterialSlotKind.Normal),
            new ProfileSlot(MaterialSlotKind.Mask),
            new ProfileSlot(MaterialSlotKind.Shadowmap)
        ),
        [MaterialTemplateProfile.AnimatedModel] = new("ModelAnimated",
            new ProfileSlot(MaterialSlotKind.Albedo),
            new ProfileSlot(MaterialSlotKind.Normal),
            new ProfileSlot(MaterialSlotKind.Mask),
            new ProfileSlot(MaterialSlotKind.Shadowmap)
        ),
        [MaterialTemplateProfile.Terrain] = new("Terrain",
            new ProfileSlot(MaterialSlotKind.Environment),
            new ProfileSlot(MaterialSlotKind.Environment),
            new ProfileSlot(MaterialSlotKind.Environment),
            new ProfileSlot(MaterialSlotKind.Environment),
            new ProfileSlot(MaterialSlotKind.Splatmap),
            new ProfileSlot(MaterialSlotKind.Shadowmap)
        )
    };
}