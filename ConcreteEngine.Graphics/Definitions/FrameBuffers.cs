namespace ConcreteEngine.Graphics;

public enum RenderBufferKind
{
    Multisample = 0,
    Color,
    DepthStencil
}

public enum FboAttachment
{
    Color,          
    Depth, 
    DepthStencil  
}