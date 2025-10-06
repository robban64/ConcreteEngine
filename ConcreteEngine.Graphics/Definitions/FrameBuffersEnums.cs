namespace ConcreteEngine.Graphics;

public enum FrameBufferAttachmentKind : byte
{
    None = 0,
    Color = 1,
    Depth = 2,
    DepthStencil = 3
}

public enum RenderBufferMsaa : byte
{
    None = 0,
    X2 = 2,
    X4 = 4,
    X8 = 8
}