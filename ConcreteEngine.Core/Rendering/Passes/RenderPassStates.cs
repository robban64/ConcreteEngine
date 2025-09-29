using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public interface IRenderPassState
{
    GfxPassClear ClearColor { get; }
    GfxPassState PassState { get; }
}

public readonly record struct EmptyState(GfxPassClear ClearColor = default, GfxPassState PassState = default)
    : IRenderPassState;

public readonly record struct ScenePassState(ShaderId ScreenShaderId) : IRenderPassState
{
    public GfxPassClear ClearColor { get; init; } = GfxPassClear.MakeColorDepthClear(Color4.CornflowerBlue);
    public int Samples { get; init; } = 4;

    public GfxPassState PassState { get; init; }
        = new(DepthTest: true, DepthWrite: true, Cull: true, Blend: false, Scissor: false, FramebufferSrgb: true);
}

public readonly record struct PostPassState(
    ShaderId EffectShaderId,
    ShaderId CompositeShaderId,
    TextureId OutputTextureId) : IRenderPassState
{
    public GfxPassClear ClearColor { get; init; } = GfxPassClear.MakeColorClear(Color4.Black);

    public GfxPassState PassState { get; init; }
        = new(DepthTest: false, DepthWrite: false, Cull: false, Blend: false, Scissor: false, FramebufferSrgb: true);
}

public readonly record struct ScreenPassState(ShaderId PresentShaderId) : IRenderPassState
{
    public GfxPassClear ClearColor { get; init; } = GfxPassClear.MakeColorClear(Color4.Black);

    public GfxPassState PassState { get; init; }
        = new(DepthTest: false, DepthWrite: false, Cull: false, Blend: false, Scissor: false, FramebufferSrgb: true);
}

public readonly record struct ResolvePassState(FrameBufferId BlitFbo) : IRenderPassState
{
    public GfxPassClear ClearColor { get; init; } = GfxPassClear.MakeColorClear(Color4.Black);
    public GfxPassState PassState { get; init; } = GfxPassState.MakeOff();
    public bool LinearFilter { get; init; } = true;
}