namespace ConcreteEngine.Graphics.Gfx.Resources;

public struct FboAttachmentIds(
    TextureId colorTextureId,
    TextureId depthTextureId,
    RenderBufferId colorRenderBufferId,
    RenderBufferId depthRenderBufferId
)
{
    public TextureId ColorTextureId = colorTextureId;
    public TextureId DepthTextureId = depthTextureId;
    public RenderBufferId ColorRenderBufferId = colorRenderBufferId;
    public RenderBufferId DepthRenderBufferId = depthRenderBufferId;
}