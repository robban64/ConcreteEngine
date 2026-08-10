using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Graphics.Gfx;

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

    protected override Material LoadInMemory(MaterialRecord record, ImportContext ctx) =>
        throw new NotImplementedException();

    protected override Material Load(MaterialRecord record, ImportContext ctx)
    {
        var mat = new Material(record.Name, ctx.Id, record.Id, record.Profile, record.Parameters);

        var sourceCount = mat.SourceCount;
        var profile = AssetManager.GetMaterialProfile(mat.ProfileId);
        for (int i = 0; i < sourceCount; i++)
        {
            var name = record.ProfileSlots.Length > i ? record.ProfileSlots[i] : null;
            if (name == null) continue;
            if (_store.TryGetByName<Texture>(name, out var tex))
                mat.SetSourceSlot(tex, profile.GetSlot(i));
        }

        return mat;
    }


    public Material LoadEmbedded(AssetId assetId, EmbeddedSceneMaterial embedded)
    {
        ArgumentException.ThrowIfNullOrEmpty(embedded.Name);

        var profile = embedded.IsAnimated ? MaterialProfileId.OpaqueAnimated : MaterialProfileId.Opaque;

        var mat = new Material(embedded.Name, assetId, embedded.GId, profile, embedded.State);

        foreach (var it in embedded.Textures)
        {
            if(!it.IsValid()) continue;

            if (!_store.TryGetByGuid<Texture>(it.GId, out var texture))
                Throwers.NotFound(it.Name, "Embedded texture");
            
            mat.SetSourceSlot(texture, it.SlotKind);
        }

        return mat;
    }
}