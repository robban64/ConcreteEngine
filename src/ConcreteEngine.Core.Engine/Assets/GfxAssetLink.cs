using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class GfxAssetLink<TMeta> where TMeta : unmanaged, IResourceMeta
{
    public readonly GfxId<TMeta> GfxId;
    public TMeta Meta;

    public GfxAssetLink(GfxId<TMeta> gfxId)
    {
        GfxId = gfxId;
        Meta = GfxResourceApi.GetMeta(gfxId);
    }
}