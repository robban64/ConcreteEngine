#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Draw;

internal sealed class DrawCommandProcessor
{
    private readonly GfxCommands _gfxCmd;
    private readonly GfxBuffers _gfxBuffers;
    private readonly RenderRegistry _registry;
    
    private readonly DrawBuffers _buffers;
    private readonly DrawStateContext _ctx;

    private RenderUbo _drawUbo = null!;
    private RenderUbo _materialUbo = null!;

    private Action<ShaderId> _applyShader;
    
    internal DrawCommandProcessor(
        DrawStateContext ctx,
        DrawStateContextPayload ctxPayload, DrawBuffers buffers)
    {
        _ctx = ctx;
        _buffers = buffers;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _gfxBuffers = ctxPayload.Gfx.Buffers;
        _registry = ctxPayload.Registry;

        _applyShader = UseShader;
    }


    public void Initialize()
    {
        _drawUbo = _registry.GetRenderUbo<DrawObjectUniform>();
        _materialUbo = _registry.GetRenderUbo<MaterialUniformRecord>();
    }

    public void PrepareFrame(nint drawCapacity, nint materialCapacity)
    {
        _ctx.ResetState();

        _drawUbo.ResetCursor();
        _materialUbo.ResetCursor();

        if (drawCapacity > _drawUbo.Capacity)
        {
            _drawUbo.SetCapacity(drawCapacity);
            _gfxBuffers.SetUniformBufferCapacity(_drawUbo.Id, drawCapacity);
        }

        if (materialCapacity > _materialUbo.Capacity)
        {
            _materialUbo.SetCapacity(materialCapacity);
            _gfxBuffers.SetUniformBufferCapacity(_materialUbo.Id, drawCapacity);
        }
    }

    private void UseShader(ShaderId shaderId) => _gfxCmd.UseShader(shaderId);

    public void PrepareDrawPass()
    {
        _drawUbo.ResetCursor();
        _ctx.ResetMaterialState();
        if (_ctx.IsDepth)
            UseShader(_ctx.CoreShaders.DepthShader);
    }

    public void DrawMesh(DrawCommand cmd, int submitIndex)
    {
        // buff
        _buffers.ApplyDrawMaterial(cmd.MaterialId, _applyShader);
        _buffers.BindDrawObject(submitIndex);
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawBoundMesh(cmd.MeshId, cmd.DrawCount);
    }
    
}