using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class MaterialLoader : AssetTypeLoader<Material, MaterialRecord>
{
    private readonly AssetStore _store;

    internal MaterialLoader()
    {
        _store = AssetManager.AssetStore;
    }


    protected override void OnActivate() { }

    protected override void OnDeActivate() { }

    internal static Material CreateFallback(AssetId assetId, Guid gId)
    {
        var param = new MaterialParams(Color4.White, 0, 0, 1);
        return new Material("Fallback", assetId, gId, default, MaterialProfile.StaticModel, in param);
    }

    protected override Material LoadInMemory(MaterialRecord record, LoaderContext ctx) =>
        throw new NotImplementedException();

    protected override Material Load(MaterialRecord record, LoaderContext ctx)
    {
        var mat = new Material(record.Name, ctx.Id, record.GId, AssetId.Empty, record.Profile, record.Parameters);

        var sourceCount = mat.SourceCount;
        for (int i = 0; i < sourceCount; i++)
        {
            var name = record.ProfileSlots.Length > i ? record.ProfileSlots[i] : null;
            if (name == null) //source.TextureKind == TextureKind.Texture2DArray
            {
                continue;
            }

            if (_store.TryGetByName<Texture>(name, out var tex))
                mat.SetSourceSlot(i, tex.Id);
        }

        return mat;
    }


    public Material LoadEmbedded(AssetId assetId, EmbeddedSceneMaterial embedded)
    {
        ArgumentException.ThrowIfNullOrEmpty(embedded.Name);

        var profile = embedded.IsAnimated ? MaterialProfile.AnimatedModel : MaterialProfile.StaticModel;

        var mat = new Material(embedded.Name, assetId, embedded.GId, AssetId.Empty, profile, in embedded.Params);

        var slot = 0;
        foreach (var textureGId in embedded.Textures)
        {
            if (!_store.TryGetByGuid<Texture>(textureGId, out var texture))
                throw new InvalidOperationException($"Embedded texture [{textureGId}] not found");

            mat.SetSourceSlot(slot++, texture.Id);
            if(slot == 3) break;
        }

        return mat;
    }

    private TextureSource[] CreateSourcesFromProfile(MaterialRecord record, MaterialProfileEntry profile)
    {
        var sources = profile.MakeSourceArray();
        for (int i = 0; i < sources.Length; i++)
        {
            var source = sources[i];
            var name = record.ProfileSlots.Length > i ? record.ProfileSlots[i] : null;
            if (name == null) //source.TextureKind == TextureKind.Texture2DArray
            {
                //sources[i] = new TextureSource(AssetId.Empty, slot);
                continue;
            }

            if (_store.TryGetByName<Texture>(name, out var tex))
                sources[i] = source with { AssetTexture = tex.Id };
        }

        return sources;
    }

    private TextureSource[] CreateSources(MaterialRecord embedded)
    {
        var sources = new TextureSource[embedded.TextureSlots.Length];

        if (embedded.TextureSlots.Length == 0)
        {
            return [new TextureSource(default, TextureUsage.Albedo)];
        }

        for (int i = 0; i < sources.Length; i++)
        {
            var slot = embedded.TextureSlots[i];
            AssetId? slotAsset = null;

            if (slot.TextureKind == TextureKind.Texture2DArray)
            {
                sources[i] = new TextureSource(default, slot.SlotKind);
                continue;
            }

            if (_store.TryGetByName<Texture>(slot.Name, out var tex))
                slotAsset = tex.Id;

            if (slotAsset is not { } slotAssetId)
                throw new InvalidOperationException($"Texture {slot.Name} does not exists for {embedded.Name}");

            sources[i] = new TextureSource(slotAssetId, slot.SlotKind);
        }

        return sources;
    }
}