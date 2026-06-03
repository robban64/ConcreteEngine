using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

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
    private static readonly Dictionary<int, MatProfileInfo> Profiles = CreateSlotProfiles();

    private readonly AssetStore _store;

    internal MaterialLoader()
    {
        _store = AssetStore.Instance;
    }


    protected override void OnActivate() { }

    protected override void OnDeActivate() { }

    internal static Material CreateFallback(AssetId assetId, Guid gId)
    {
        TextureSource[] slots = [new(AssetId.Empty, TextureUsage.Albedo)];
        var param = new MaterialParams(Color4.White, 0, 0, 1);
        return new Material("Fallback", assetId, gId, default, null, MaterialProfile.None, in param, slots);
    }

    protected override Material LoadInMemory(MaterialRecord record, LoaderContext ctx) =>
        throw new NotImplementedException();

    protected override Material Load(MaterialRecord record, LoaderContext ctx)
    {
        var slots = Array.Empty<TextureSource>();

        string? shaderName = null;

        if (record.TextureSlots.Length > 0)
            slots = CreateSources(record);
        else if (record.Profile != MaterialProfile.None)
        {
            var profile = Profiles[(int)record.Profile];
            shaderName = profile.Shader;
            slots = CreateSlotsFromProfile(profile.Slots, record);
        }

        if (record.Shader != null) shaderName = record.Shader;

        if (string.IsNullOrEmpty(shaderName))
            throw new InvalidOperationException($"Missing shader name for material {record.Name}");

        var shader = _store.GetByName<Shader>(shaderName);

        return new Material(record.Name, ctx.Id, record.GId, AssetId.Empty, shader, record.Profile, record.Parameters,
            slots);
    }


    public Material LoadEmbedded(AssetId assetId, EmbeddedSceneMaterial embedded)
    {
        ArgumentException.ThrowIfNullOrEmpty(embedded.Name);

        TextureSource[] slots =
        [
            new(default, TextureUsage.Albedo),
            new(default, TextureUsage.Normal),
            new(default, TextureUsage.Mask),
        ];

        foreach (var (textureGId, textureIndex) in embedded.Textures)
        {
            if (!_store.TryGetByGuid<Texture>(textureGId, out var texture))
                throw new InvalidOperationException($"Embedded texture [{textureIndex}] not found: {textureGId}");

            if (texture.Usage == TextureUsage.Albedo)
                slots[0] = slots[0].WithAssetId(texture.Id);

            if (texture.Usage == TextureUsage.Normal)
                slots[1] = slots[1].WithAssetId(texture.Id);
        }


        var matProfile = embedded.IsAnimated ? MaterialProfile.AnimatedModel : MaterialProfile.StaticModel;
        var profile = Profiles[(int)matProfile];
        var shader = _store.GetByName<Shader>(profile.Shader);

        return new Material(embedded.Name, assetId, embedded.GId, AssetId.Empty, shader, matProfile, in embedded.Params,
            slots);
    }


    private TextureSource[] CreateSources(MaterialRecord embedded)
    {
        if (embedded.TextureSlots.Length == 0)
        {
            return [new TextureSource(default, TextureUsage.Albedo)];
        }

        var sources = new TextureSource[embedded.TextureSlots.Length];
        for (int i = 0; i < sources.Length; i++)
        {
            var slot = embedded.TextureSlots[i];
            AssetId? slotAsset = null;

            if (slot.TextureKind == TextureKind.Texture2DArray)
            {
                sources[i] = new TextureSource(default, slot.SlotKind, slot.TextureKind);
                continue;
            }

            if (_store.TryGetByName<Texture>(slot.Name, out var tex))
                slotAsset = tex.Id;

            if (slotAsset is not { } slotAssetId)
                throw new InvalidOperationException($"Texture {slot.Name} does not exists for {embedded.Name}");

            sources[i] = new TextureSource(slotAssetId, slot.SlotKind, slot.TextureKind);
        }

        return sources;
    }

    private TextureSource[] CreateSlotsFromProfile(ProfileSlot[] profile, MaterialRecord desc)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var slots = new TextureSource[profile.Length];
        for (int i = 0; i < profile.Length; i++)
        {
            var info = profile[i];
            var name = desc.ProfileSlots.Length > i ? desc.ProfileSlots[i] : null;
            if (name == null)
            {
                slots[i] = new TextureSource(AssetId.Empty, info.SlotKind, info.TexKind);
                continue;
            }

            var tex = _store.GetByName<Texture>(name);
            slots[i] = new TextureSource(tex.Id, info.SlotKind, info.TexKind);
        }

        return slots;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<int, MatProfileInfo> CreateSlotProfiles() =>
        new()
        {
            [(int)MaterialProfile.None] = new MatProfileInfo("Model"),
            [(int)MaterialProfile.Foliage] = new MatProfileInfo("Foliage", new ProfileSlot(TextureUsage.Albedo)),
            [(int)MaterialProfile.Particle] = new MatProfileInfo("Particle", new ProfileSlot(TextureUsage.Albedo)),
            [(int)MaterialProfile.Sky] =
                new MatProfileInfo("Skybox", new ProfileSlot(TextureUsage.Albedo, TextureKind.CubeMap)),
            [(int)MaterialProfile.StaticModel] = new MatProfileInfo("Model",
                new ProfileSlot(TextureUsage.Albedo),
                new ProfileSlot(TextureUsage.Normal),
                new ProfileSlot(TextureUsage.Mask)
            ),
            [(int)MaterialProfile.AnimatedModel] = new MatProfileInfo("ModelAnimated",
                new ProfileSlot(TextureUsage.Albedo),
                new ProfileSlot(TextureUsage.Normal),
                new ProfileSlot(TextureUsage.Mask)
            )
        };
}