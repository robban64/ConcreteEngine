namespace ConcreteEngine.Graphics.Resources;

public readonly record struct FboAttachmentIds(
    TextureId ColorTextureId,
    TextureId DepthTextureId,
    RenderBufferId ColorRenderBufferId,
    RenderBufferId DepthRenderBufferId
);