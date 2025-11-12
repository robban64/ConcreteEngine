#region

using ConcreteEngine.Graphics.Diagnostic;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal delegate GfxMetaSpecialMetric GetSpecialMetric<TMeta>(ReadOnlySpan<TMeta> metas)
    where TMeta : unmanaged, IResourceMeta;

internal delegate void BackendDeleteDel(in DeleteResourceCommand cmd);

internal delegate BackendResourceStore<TId, THandle> GetBackendStoreDel<TId, THandle>()
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle> where TId : unmanaged, IResourceId;

internal delegate GfxResourceStore<TId, TMeta> GetGfxStoreDel<TId, TMeta>()
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta;

public delegate void GfxMetaChangedDel<in TId, TMeta>(TId id, in TMeta newMeta, in TMeta oldMeta, GfxMetaChanged message)
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta;