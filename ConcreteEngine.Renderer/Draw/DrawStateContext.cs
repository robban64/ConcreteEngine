using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Renderer.Draw;

internal sealed class DrawStateContextPayload
{
    public required RenderRegistry Registry { get; init; }
    public required RenderCamera RenderCamera { get; init; }
    public required RenderParamsSnapshot Snapshot { get; init; }
    public required GfxContext Gfx { get; init; }
}

internal sealed class DrawStateContext
{
    private readonly RenderShaderRegistry _shaderRegistry;

    public TextureId DepthTexture { get; private set; }
    public PassStateMode PassMode { get; private set; }
    public MaterialId PrevMaterial { get; private set; } = new(-1);

    public MeshId FsqMesh { get; }

    public GfxPassState PassState;
    public GfxPassStateFunc PassStateFunc;

    public GfxPassState OverridePassState = default;
    public GfxPassStateFunc OverridePassStateFunc = default;

    internal DrawStateContext(RenderRegistry registry, MeshId fsqMesh)
    {
        var depthFbo = registry.GetRenderFbo(TagRegistry.FboKey<ShadowPassTag>(FboVariant.Default));

        FsqMesh = fsqMesh;
        DepthTexture = depthFbo.Attachments.DepthTextureId;
        _shaderRegistry = registry.ShaderRegistry;
    }

    
    public ref readonly RenderCoreShaders CoreShaders => ref _shaderRegistry.CoreShaders;

    public ReadOnlySpan<int> GetUniformLocations(ShaderId shader) =>
        _shaderRegistry.GetRenderShader(shader).GetUniforms();

    public bool IsMain => PassMode == PassStateMode.Main;
    public bool IsDepth => PassMode == PassStateMode.Depth;

    public void SetDepthMode() => PassMode = PassStateMode.Depth;

    public void ResetPassMode() => PassMode = PassStateMode.Main;
    public void ResetMaterialState() => PrevMaterial = default;

    public void ResetState()
    {
        PrevMaterial = default;
        PassMode = PassStateMode.Main;

        PassState = default;
        PassStateFunc = default;
        OverridePassState = default;
        OverridePassStateFunc = default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ResolveMaterialBind(MaterialId material)
    {
        if (material == PrevMaterial) return false;
        PrevMaterial = material;
        return true;
    }

    public ShaderId ResolveShaderPolicy(ShaderId cmdShader) =>
        PassMode switch
        {
            PassStateMode.Main => cmdShader,
            PassStateMode.Post => cmdShader,
            PassStateMode.Depth => CoreShaders.DepthShader,
            _ => throw new ArgumentOutOfRangeException()
        };
}