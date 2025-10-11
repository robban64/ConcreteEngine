#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Draw;

internal sealed class DrawProcessor
{
    private readonly GfxCommands _gfxCmd;
    private readonly GfxBuffers _gfxBuffers;
    private readonly RenderRegistry _registry;

    private readonly MaterialStore _materials;

    private readonly DrawStateContext _ctx;

    private int _previousMaterialId = -1;

    private RenderUbo _drawUbo = null!;

    private UniformBufferId _materialUboId;

    internal DrawProcessor(DrawStateContext ctx, DrawStateContextPayload ctxPayload, MaterialStore materials)
    {
        _ctx = ctx;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _gfxBuffers = ctxPayload.Gfx.Buffers;
        _registry = ctxPayload.Registry;
        _materials = materials;
    }


    public void Initialize()
    {
        _drawUbo = _registry.GetRenderUbo<DrawObjectUniform>();
        _materialUboId = _registry.GetRenderUbo<MaterialUniformRecord>().Id;
    }

    public void PrepareFrame(in RenderSceneState renderGlobals, nint capacity)
    {
        _drawUbo.ResetCursor();
        if (capacity != _drawUbo.Capacity)
            _drawUbo.SetCapacity(capacity);

        _previousMaterialId = -1;
        _gfxBuffers.SetUniformBufferCapacity(_drawUbo.Id, capacity);
    }

    public void PrepareDrawPass()
    {
        _drawUbo.ResetCursor();
        _previousMaterialId = -1;
        if (_ctx.OverrideDrawShader > 0)
            UseShader(_ctx.OverrideDrawShader);
    }

    private void UseShader(ShaderId shaderId)
    {
        //var renderShader = _registry.GetRenderShader(shaderId);
        _gfxCmd.UseShader(shaderId);
    }

    private void BindDrawMaterial(MaterialId materialId)
    {
        if (_previousMaterialId == materialId.Id) return;
        var material = _materials.GetMaterial(materialId);
        UseShader(material.ShaderId);

        for (var i = 0; i < material.SamplerSlots.Length; i++)
        {
            var value = material.SamplerSlots[i];
            _gfxCmd.BindTexture(value, i);
        }

        if (material.Shadows)
            _gfxCmd.BindTexture(_ctx.DepthTexture, material.SamplerSlots.Length);

        UploadMaterial(material);

        _previousMaterialId = materialId.Id;
    }

    public void UploadMaterial(Material mat)
    {
        var data = new MaterialUniformRecord(
            matColor: new Vector4(mat.Color.AsVec3(), 1),
            matParams0: new Vector4(mat.SpecularStrength, mat.UvRepeat, 0.0f, 0.0f),
            matParams1: new Vector4(mat.Shininess, mat.HasNormalMap ? 1.0f : 0.0f, 0.0f, 0.0f)
        );

        _gfxBuffers.UploadUniformGpuData(_materialUboId, in data, 0);
    }

    //TODO bulk upload
    public void UploadTransform(in DrawTransformPayload payload, int submitIndex)
    {
        TransformUtils.CreateNormalMatrix(in payload.Transform, out var normalModel);

        var data = new DrawObjectUniform(
            model: in payload.Transform,
            normal: in normalModel
        );

        _gfxBuffers.UploadUniformGpuData(_drawUbo.Id, in data, _drawUbo.SetUploadCursor(submitIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BindDrawObject(int submitIndex)
    {
        _gfxBuffers.BindUniformBufferRange(_drawUbo.Id, _drawUbo.SetDrawCursor(submitIndex), _drawUbo.Stride);
    }

    public void DrawMesh(DrawCommand cmd, int submitIndex)
    {
        if (_ctx.OverrideDrawShader == default)
            BindDrawMaterial(cmd.MaterialId);

        BindDrawObject(submitIndex);
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawBoundMesh(cmd.MeshId, cmd.DrawCount);
    }
}