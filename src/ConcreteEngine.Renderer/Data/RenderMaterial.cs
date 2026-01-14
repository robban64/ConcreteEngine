using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Renderer.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderMaterialPayload(
    MaterialId materialId,
    ShaderId shaderId,
    in MaterialParams param,
    MaterialProperties props,
    MaterialPipeline pipeline)
{
    public readonly MaterialParams Param = param;
    public readonly MaterialProperties Props = props;
    public readonly MaterialPipeline Pipeline = pipeline;
    public readonly ShaderId ShaderId = shaderId;
    public readonly MaterialId MaterialId = materialId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteTo(ref RenderMaterialMeta meta, ref MaterialUniformRecord record)
    {
        meta = new RenderMaterialMeta(MaterialId, ShaderId, Pipeline.PassState, Pipeline.PassFunctions);

        float transparency = Props.HasTransparency ? 1f : 0f;
        float normal = Props.HasNormal ? 1f : 0f;
        float alpha = Props.HasAlphaMask ? 1f : 0f;

        record.MatColor = Param.Color;
        record.MatParams0 = new Vector4(Param.Specular, Param.UvRepeat, 1.0f, 1.0f);
        record.MatParams1 = new Vector4(Param.Shininess, normal, transparency, alpha);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RenderMaterialMeta(
    MaterialId materialId,
    ShaderId shaderId,
    GfxPassState passState,
    GfxPassFunctions passFunctions)
{
    public readonly ShaderId ShaderId = shaderId;
    public readonly MaterialId MaterialId = materialId;
    public readonly GfxPassState PassState = passState;
    public readonly GfxPassFunctions PassFunctions = passFunctions;
}