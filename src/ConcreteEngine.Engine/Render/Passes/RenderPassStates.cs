namespace ConcreteEngine.Engine.Render.Passes;

public struct RenderPassParams
{
    public FrameBufferId Target;
    public FrameBufferId ResolveTarget;
    
    public ShaderId PassShader;
    public bool LinearFilter;
}