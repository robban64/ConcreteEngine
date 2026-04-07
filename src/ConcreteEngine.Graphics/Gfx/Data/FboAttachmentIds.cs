using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Data;

public struct FboAttachmentIds(
    TextureId colorTexture,
    TextureId depthTexture,
    RenderBufferId colorRbo,
    RenderBufferId depthRbo
)
{
    public TextureId ColorTexture = colorTexture;
    public TextureId DepthTexture = depthTexture;
    public RenderBufferId ColorRbo = colorRbo;
    public RenderBufferId DepthRbo = depthRbo;
}