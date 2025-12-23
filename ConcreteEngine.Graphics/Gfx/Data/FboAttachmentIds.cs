using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Data;

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