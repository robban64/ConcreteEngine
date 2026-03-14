using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class MaterialLoader : AssetTypeLoader<Material, MaterialRecord>
{
    //
    private sealed class MatProfileInfo(string shader, params ProfileSlot[] slots)
    {
        public readonly string Shader = shader;
        public readonly ProfileSlot[] Slots = slots;
    }

    private readonly struct ProfileSlot(TextureUsage slotKind, TextureKind texKind = TextureKind.Texture2D)
    {
        public readonly TextureUsage SlotKind = slotKind;
        public readonly TextureKind TexKind = texKind;
    }
    //

    private Dictionary<MaterialProfile, MatProfileInfo> _profiles;

    private AssetStore _store;

    internal MaterialLoader(AssetStore store, AssetGfxUploader gfxUploader) : base(gfxUploader)
    {
        _store = store;
        _profiles = CreateSlotProfiles();
    }

    public override void Setup()
    {
        IsActive = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Teardown()
    {
        _profiles.Clear();
        _profiles = null!;
        _store = null!;
        IsActive = false;
    }
    
    internal static Material CreateFallback(AssetId assetId, Guid gId)
    {
        TextureSource[] slots = [new (new AssetId(0), TextureUsage.Albedo)];
        var param  = new MaterialParams(Color4.White, 0, 0, 1);
        return new Material("Fallback", AssetId.Empty, AssetId.Empty, in param, slots)
        {
            Id = assetId, GId = gId,
        };
    }

    protected override Material Load(MaterialRecord record, LoaderContext ctx)
    {
        var slots = Array.Empty<TextureSource>();

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

        var shader = _store.GetByName<Shader>(shaderName).Id;

        return new Material(record.Name, AssetId.Empty, shader, record.Parameters, slots)
        {
            Id = ctx.Id, GId = record.GId, AssetShader = shader,
        };
    }

    public Material LoadEmbedded(AssetId assetId, EmbeddedSceneMaterial embedded)
    {
        ArgumentException.ThrowIfNullOrEmpty(embedded.Name);

        TextureSource[] slots =
        [
            new(default, TextureUsage.Albedo),
            new(default, TextureUsage.Normal),
            new(default, TextureUsage.Mask),
            new(default, TextureUsage.Shadowmap),
        ];

        foreach (var (textureGId, textureIndex) in embedded.Textures)
        {
            if (!_store.TryGetByGuid(textureGId, out Texture texture))
                throw new InvalidOperationException($"Embedded texture [{textureIndex}] not found: {textureGId}");

            if (texture.Usage == TextureUsage.Albedo)
                slots[0] = slots[0].WithAssetId(texture.Id);

            if (texture.Usage == TextureUsage.Normal)
                slots[1] = slots[1].WithAssetId(texture.Id);
        }

        var shaderName = embedded.IsAnimated ? "ModelAnimated" : "Model";

        var shader = _store.GetByName<Shader>(shaderName).Id;
        return new Material(embedded.Name, AssetId.Empty, shader, in embedded.Params, slots)
        {
            Id = assetId, GId = embedded.GId,
        };
    }
    


    private TextureSource[] CreateSlots(MaterialRecord embedded)
    {
        if (embedded.TextureSlots.Length == 0)
        {
            return [new TextureSource(default, TextureUsage.Albedo)];
        }

        var slotInfo = new TextureSource[embedded.TextureSlots.Length];
        for (int i = 0; i < slotInfo.Length; i++)
        {
            var slot = embedded.TextureSlots[i];
            AssetId? slotAsset = null;

            if (slot.SlotKind == TextureUsage.Shadowmap)
            {
                slotInfo[i] = new TextureSource(default, slot.SlotKind, slot.TextureKind);
                continue;
            }

            if (_store.TryGetByName<Texture>(slot.Name, out var tex))
                slotAsset = tex!.Id;

            if (slotAsset is not { } slotAssetId)
                throw new InvalidOperationException($"Texture {slot.Name} does not exists for {embedded.Name}");

            slotInfo[i] = new TextureSource(slotAssetId, slot.SlotKind, slot.TextureKind);
        }

        return slotInfo;
    }

    private TextureSource[] CreateSlotsFromProfile(ProfileSlot[] profile, MaterialRecord desc)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var slots = new List<TextureSource>();

        for (int i = 0; i < profile.Length; i++)
        {
            var info = profile[i];
            var name = desc.ProfileSlots.Length > i ? desc.ProfileSlots[i] : null;
            if (name == null)
            {
                slots.Add(new TextureSource(new AssetId(0), info.SlotKind, info.TexKind));
                continue;
            }

            var tex = _store.GetByName<Texture>(name);
            slots.Add(new TextureSource(tex!.Id, info.SlotKind, info.TexKind));
        }

        return slots.ToArray();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<MaterialProfile, MatProfileInfo> CreateSlotProfiles() =>
        new()
        {
            [MaterialProfile.None] = new MatProfileInfo("Model"),
            [MaterialProfile.Particle] = new MatProfileInfo("Particle", new ProfileSlot(TextureUsage.Albedo)),
            [MaterialProfile.Sky] =
                new MatProfileInfo("Skybox", new ProfileSlot(TextureUsage.Albedo, TextureKind.CubeMap)),
            [MaterialProfile.StaticModel] = new MatProfileInfo("Model",
                new ProfileSlot(TextureUsage.Albedo),
                new ProfileSlot(TextureUsage.Normal),
                new ProfileSlot(TextureUsage.Mask),
                new ProfileSlot(TextureUsage.Shadowmap)
            ),
            [MaterialProfile.AnimatedModel] = new MatProfileInfo("ModelAnimated",
                new ProfileSlot(TextureUsage.Albedo),
                new ProfileSlot(TextureUsage.Normal),
                new ProfileSlot(TextureUsage.Mask),
                new ProfileSlot(TextureUsage.Shadowmap)
            ),
            [MaterialProfile.Terrain] = new MatProfileInfo("Terrain",
                new ProfileSlot(TextureUsage.Environment),
                new ProfileSlot(TextureUsage.Environment),
                new ProfileSlot(TextureUsage.Environment),
                new ProfileSlot(TextureUsage.Environment),
                new ProfileSlot(TextureUsage.Splatmap),
                new ProfileSlot(TextureUsage.Shadowmap)
            )
        };
}