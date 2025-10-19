#region

using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Renderer.Draw;

internal sealed class DrawStateContextPayload
{
    public required RenderRegistry Registry { get; init; }
    public required RenderView RenderView { get; init; }
    public required RenderSceneSnapshot Snapshot { get; init; }
    public required GfxContext Gfx { get; init; }
}

internal sealed class DrawStateContext
{
    public TextureId DepthTexture { get; private set; }
    public PassStateMode PassState { get; private set; }
    public MaterialId PrevMaterial { get; private set; } = new(-1);
    
    public MeshId FsqMesh { get;  }

    public readonly RenderCoreShaders CoreShaders;

    internal DrawStateContext(RenderRegistry registry, MeshId fsqMesh)
    {
        FsqMesh = fsqMesh;
        var depthFbo = registry.GetRenderFbo(TagRegistry.FboKey<ShadowPassTag>(FboVariant.Default));
        DepthTexture = depthFbo.Attachments.DepthTextureId;
        CoreShaders = registry.ShaderRegistry.CoreShaders;
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
        PassStateMode.Depth => CoreShaders.DepthShader,
        _ => throw new ArgumentOutOfRangeException()
    };
}