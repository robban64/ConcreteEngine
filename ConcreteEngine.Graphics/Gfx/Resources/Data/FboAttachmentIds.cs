using ConcreteEngine.Graphics.Gfx.Resources.Handles;

namespace ConcreteEngine.Graphics.Gfx.Resources.Data;

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