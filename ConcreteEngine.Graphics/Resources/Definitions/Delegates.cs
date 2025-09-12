namespace ConcreteEngine.Graphics.Resources;


internal delegate TId MakeIdDelegate<out TId>(int handle) where TId : struct, IResourceId;
