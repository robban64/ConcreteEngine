namespace ConcreteEngine.Engine.RenderPipeline;

public struct RenderPassParams
{
    public FrameBufferId Target;
    public FrameBufferId ResolveTarget;

    public ShaderId PassShader;
    public bool LinearFilter;
}