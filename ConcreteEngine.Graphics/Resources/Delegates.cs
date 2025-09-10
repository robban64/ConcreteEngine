namespace ConcreteEngine.Graphics.Resources;


internal delegate TId MakeIdDelegate<out TId>(int handle) where TId : struct, IResourceId;

internal delegate TId CreateStoreDel<THandle, TMeta, out TId>(in TMeta meta, in THandle handle)
    where TId : struct, IResourceId where TMeta : struct where THandle : struct;

internal delegate TId UploadFboDel<TId,  TMeta, in THandle>(TId id, in TMeta newMeta,  THandle newHandle)
    where TId : struct, IResourceId where TMeta : struct where THandle : struct;
