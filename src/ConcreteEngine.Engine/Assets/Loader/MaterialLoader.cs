using System.Diagnostics;
using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Assets.Materials;

internal sealed class MaterialLoader : AssetTypeLoader<MaterialTemplate, MaterialRecord>
{
    private readonly AssetStore _store;

    private sealed record MatProfileInfo(string Shader, params ProfileSlot[] Slots);

    private readonly record struct ProfileSlot(TextureSlotKind SlotKind, TextureKind TexKind = TextureKind.Texture2D);

    private readonly Dictionary<MaterialProfile, MatProfileInfo> _profiles;

    internal MaterialLoader(AssetStore store, AssetGfxUploader gfxUploader) : base(gfxUploader)
    {
        _store = store;
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

    protected override MaterialTemplate Load(MaterialRecord record, LoaderContext ctx)
    {
        var slots = Array.Empty<AssetTextureSlot>();

        string? shaderName = null;

        if (record.TextureSlots.Length > 0)
            slots = CreateSlots(record);
        else if (record.Profile != MaterialProfile.None)
        {
            var profile = _profiles[record.Profile];
            shaderName = profile.Shader;
            slots = CreateSlotsFromProfile(profile.Slots, record);
        }

        if (record.Shader != null) shaderName = record.Shader;

        if (string.IsNullOrEmpty(shaderName))
            throw new InvalidOperationException($"Missing shader name for material {record.Name}");

        var shader = _store.GetByName<Shader>(shaderName).RefId;

        var matParams = new MaterialState(record.Parameters);
        return new MaterialTemplate(slots)
        {
            Id = ctx.Id,
            GId = ctx.GId,
            Name = record.Name,
            ShaderRef = shader,
            Params = matParams
        };
    }

    protected override MaterialTemplate LoadEmbedded(EmbeddedRecord embedded, LoaderContext context)
    {
        var desc = (MaterialEmbeddedRecord)embedded;
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

            if (!_store.TryGetByGuid(gid, out Texture2D texture))
                throw new ArgumentException($"Embedded texture {textureIndex}  not found: {gid}");

            if (texture.SlotKind == TextureSlotKind.Albedo)
                slots[0] = slots[0].WithAssetId(texture.Id);

            if (texture.SlotKind == TextureSlotKind.Normal)
                slots[1] = slots[1].WithAssetId(texture.Id);
        }

        var shaderName = desc.IsAnimated ? "ModelAnimated" : "Model";

        var matParams = new MaterialState(in desc.Data, desc.Props);
        return new MaterialTemplate(slots)
        {
            Id = context.Id,
            GId = context.GId,
            Name = desc.AssetName,
            ShaderRef = _store.GetByName<Shader>(shaderName).RefId,
            Params = matParams
        };
    }

    public override void Teardown() { }


    private AssetTextureSlot[] CreateSlots(MaterialRecord embedded)
    {
        if (embedded.TextureSlots.Length == 0)
        {
            return [new AssetTextureSlot(default, TextureSlotKind.Albedo)];
        }

        var slotInfo = new AssetTextureSlot[embedded.TextureSlots.Length];
        for (int i = 0; i < slotInfo.Length; i++)
        {
            var slot = embedded.TextureSlots[i];
            AssetId? slotAsset = null;

            if (slot.SlotKind == TextureSlotKind.Shadowmap)
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
}