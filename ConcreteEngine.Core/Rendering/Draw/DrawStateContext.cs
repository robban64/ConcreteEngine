#region

using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;

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
    public ShaderId DepthShader { get; }
    public TextureId DepthTexture { get; private set; }
    public StateModeKind StateMode { get; set; }

    internal DrawStateContext(ShaderId depthShader, TextureId depthTexture)
    {
        DepthShader = depthShader;
        DepthTexture = depthTexture;
    }
    
    public bool IsMain => StateMode == StateModeKind.Main;
    public bool IsDepth => StateMode == StateModeKind.Depth;
    
    public void RestoreStateMode() => StateMode = StateModeKind.Main;
    public void SetDepthMode() => StateMode = StateModeKind.Depth;

    public ShaderId ResolveShaderPolicy(ShaderId cmdShader) => StateMode switch
    {
        StateModeKind.Main => cmdShader,
        StateModeKind.Post => cmdShader,
        StateModeKind.Depth => DepthShader,
        _ => throw new ArgumentOutOfRangeException()
    };
    

}