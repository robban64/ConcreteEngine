namespace ConcreteEngine.Graphics.Resources;

internal delegate TId MakeIdDelegate<out TId>(int handle) where TId : unmanaged, IResourceId;

internal delegate void BackendDelete(in DeleteCmd cmd);

public delegate ref readonly TMeta GetMetaDel<in TId,TMeta>(TId id);
