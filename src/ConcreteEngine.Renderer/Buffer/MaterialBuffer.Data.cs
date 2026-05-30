using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Renderer.Buffer;

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderMaterialPayload(
    MaterialId materialId,
    ShaderId shaderId,
    in MaterialParams param,
    MaterialRenderProps props,
    MaterialPipeline pipeline)
{
    public readonly MaterialParams Param = param;
    public readonly MaterialRenderProps Props = props;
    public readonly MaterialPipeline Pipeline = pipeline;
    public readonly ShaderId ShaderId = shaderId;
    public readonly MaterialId MaterialId = materialId;

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteTo(scoped ref RenderMaterialMeta meta, scoped ref MaterialUniform record)
    {
        meta = new RenderMaterialMeta(MaterialId, ShaderId, Pipeline.DrawState, Pipeline.PassFunctions, Props.HasShadowMap);

        float transparency = Props.HasTransparency ? 1f : 0f;
        float normal = Props.HasNormal ? 1f : 0f;
        float alpha = Props.HasAlphaMask ? 1f : 0f;

        record.MatColor = Param.Color;
        record.MatParams0 = new Vector4(Param.Specular, Param.UvRepeat, 1.0f, 1.0f);
        record.MatParams1 = new Vector4(Param.Shininess, normal, transparency, alpha);
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderMaterialMeta(
    MaterialId materialId,
    ShaderId shaderId,
    GfxDrawState drawState,
    GfxPassFunctions passFunctions,
    bool shadowMapping)
{
    public readonly GfxDrawState DrawState = drawState;
    public readonly GfxPassFunctions PassFunctions = passFunctions;
    public readonly ShaderId ShaderId = shaderId;
    public readonly MaterialId MaterialId = materialId;
    public readonly bool ShadowMapping = shadowMapping;
}