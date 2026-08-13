using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using static ConcreteEngine.Graphics.Gfx.GfxStateFlags;

namespace ConcreteEngine.Engine.Render.Passes;

public struct RenderPassState(
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