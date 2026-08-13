namespace ConcreteEngine.Engine.Render.Passes;

public struct PassState(
    FrameBufferId target,
    ShaderId passShader = default,
    FrameBufferId resolveTarget = default,
    bool linearFilter = false
)
{
    public FrameBufferId Target = target;
    public FrameBufferId ResolveTarget = resolveTarget;
    
    public ShaderId PassShader = passShader;
    public bool LinearFilter = linearFilter;

}