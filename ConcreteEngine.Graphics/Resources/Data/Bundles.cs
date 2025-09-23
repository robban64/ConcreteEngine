namespace ConcreteEngine.Graphics.Resources;

internal readonly record struct DriverHandleMeta<TMeta>(in GfxHandle Handle, in TMeta Meta)
    where TMeta : unmanaged, IResourceMeta;

internal readonly record struct IdMetaResource<TId, TMeta>
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta;

internal readonly struct DriverCreateFboResult(
    in DriverHandleMeta<FrameBufferMeta> fbo,
    in DriverHandleMeta<TextureMeta> fboTex,
    in DriverHandleMeta<RenderBufferMeta> rboDepth,
    in DriverHandleMeta<RenderBufferMeta> rboTex)
{
    public readonly DriverHandleMeta<FrameBufferMeta> Fbo = fbo;
    public readonly DriverHandleMeta<TextureMeta> FboTex = fboTex;
    public readonly DriverHandleMeta<RenderBufferMeta> RboDepth = rboDepth;
    public readonly DriverHandleMeta<RenderBufferMeta> RboTex = rboTex;
}

internal readonly struct GfxCreateFboResult(
    in IdMetaResource<FrameBufferId, FrameBufferMeta> fbo,
    in IdMetaResource<TextureId, TextureMeta> fboTex,
    in IdMetaResource<RenderBufferId, RenderBufferMeta> rboDepth,
    in IdMetaResource<RenderBufferId, RenderBufferMeta> rboTex)
{
    public readonly IdMetaResource<FrameBufferId, FrameBufferMeta> Fbo = fbo;
    public readonly IdMetaResource<TextureId, TextureMeta> FboTex = fboTex;
    public readonly IdMetaResource<RenderBufferId, RenderBufferMeta> RboDepth = rboDepth;
    public readonly IdMetaResource<RenderBufferId, RenderBufferMeta> RboTex = rboTex;
}