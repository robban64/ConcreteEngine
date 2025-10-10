#region

using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Draw;

internal sealed class DrawStateContextPayload
{
    public required RenderRegistry Registry { get; init; }
    public required RenderView RenderView { get; init; }
    public required RenderSceneState Snapshot { get; init; }
    public required GfxContext Gfx { get; init; }
}

internal sealed class DrawStateContext
{
    public enum StateModeKind
    {
        Main,
        Depth,
        Post
    }

    private readonly ShaderId _depthShader;

    public ShaderId OverrideDrawShader { get; private set; }
    public TextureId DepthTexture { get; private set; }
    public StateModeKind StateMode { get; set; }


    internal DrawStateContext(ShaderId depthShader, TextureId depthTexture)
    {
        _depthShader = depthShader;
        DepthTexture = depthTexture;
    }

    public void SetDepthMode()
    {
        OverrideDrawShader = _depthShader;
        StateMode = StateModeKind.Depth;
    }

    public void RestoreStateMode()
    {
        OverrideDrawShader = default;
        StateMode = StateModeKind.Main;
    }
}