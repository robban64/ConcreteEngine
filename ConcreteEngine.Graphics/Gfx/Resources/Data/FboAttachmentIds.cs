namespace ConcreteEngine.Graphics.Gfx.Resources;

public readonly record struct FboAttachmentIds(
    TextureId ColorTextureId,
    TextureId DepthTextureId,
    RenderBufferId ColorRenderBufferId,
    RenderBufferId DepthRenderBufferId
);