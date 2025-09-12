namespace ConcreteEngine.Graphics.Resources;

internal readonly record struct GlTextureHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlShaderHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlMeshHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlVboHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlIboHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlFboHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlRboHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlUboHandle(uint Handle) : IResourceHandle;