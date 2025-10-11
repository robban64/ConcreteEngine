namespace ConcreteEngine.Graphics.Resources;

internal delegate TId MakeIdDelegate<out TId>(int handle) where TId : unmanaged, IResourceId;

internal delegate void BackendDelete(in DeleteResourceCommand cmd);

public delegate void GfxMetaChangedDel<in TId, TMeta>(TId id, in GfxMetaChanged<TMeta> message)
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta;