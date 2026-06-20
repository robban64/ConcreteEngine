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
        _store = AssetManager.Assets;
    }


    protected override void OnActivate() { }

    protected override void OnDeActivate() { }

    protected override Material LoadInMemory(MaterialRecord record, LoaderContext ctx) =>
        throw new NotImplementedException();

    protected override Material Load(MaterialRecord record, LoaderContext ctx)
    {
        var mat = new Material(record.Name, ctx.Id, record.Id, record.Profile, record.Parameters);

        var sourceCount = mat.SourceCount;
        for (int i = 0; i < sourceCount; i++)
        {
            var name = record.ProfileSlots.Length > i ? record.ProfileSlots[i] : null;
            if (name == null) continue;
            if (_store.TryGetByName<Texture>(name, out var tex))
                mat.SetSourceSlot(i, tex.Id);
        }

        return mat;
    }


    public Material LoadEmbedded(AssetId assetId, EmbeddedSceneMaterial embedded)
    {
        ArgumentException.ThrowIfNullOrEmpty(embedded.Name);

        var profile = embedded.IsAnimated ? MaterialProfileId.OpaqueAnimated : MaterialProfileId.Opaque;

        var mat = new Material(embedded.Name, assetId, embedded.GId, profile, embedded.State);

        var slot = 0;
        foreach (var textureGId in embedded.Textures)
        {
            if (!_store.TryGetByGuid<Texture>(textureGId, out var texture))
                throw new InvalidOperationException($"Embedded texture [{textureGId}] not found");

            mat.SetSourceSlot(slot++, texture.Id);
            if (slot == 3) break;
        }

        return mat;
    }
}