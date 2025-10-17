#region

using ConcreteEngine.Core.Rendering.Data;
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
    public PassStateMode PassState { get; private set; }

    public MaterialId PrevMaterial { get; private set; } = new (-1);

    internal DrawStateContext(ShaderId depthShader, TextureId depthTexture)
    {
        DepthShader = depthShader;
        DepthTexture = depthTexture;
    }
    
    public bool IsMain => PassState == PassStateMode.Main;
    public bool IsDepth => PassState == PassStateMode.Depth;
    
    public void SetDepthMode() => PassState = PassStateMode.Depth;

    public void ResetPassMode() => PassState = PassStateMode.Main;
    public void ResetMaterialState() => PrevMaterial = default;

    public void ResetState()
    {
        PrevMaterial = default;
        PassState = PassStateMode.Main;
    }

    public bool ResolveMaterialBind(MaterialId material)
    {
        if (material == PrevMaterial) return false;
        PrevMaterial = material;
        return true;
    } 
    public ShaderId ResolveShaderPolicy(ShaderId cmdShader) => PassState switch
    {
        PassStateMode.Main => cmdShader,
        PassStateMode.Post => cmdShader,
        PassStateMode.Depth => DepthShader,
        _ => throw new ArgumentOutOfRangeException()
    };
    

}