#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Materials;
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
    private readonly RenderRegistry _registry;

    private readonly DrawBuffers _buffers;
    private readonly DrawStateContext _ctx;

    private readonly Action<ShaderId> _applyShader;

    internal DrawCommandProcessor(
        DrawStateContext ctx,
        DrawStateContextPayload ctxPayload,
        DrawBuffers buffers)
    {
        _ctx = ctx;
        _buffers = buffers;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _registry = ctxPayload.Registry;

        _applyShader = UseShader;
    }


    public void Initialize()
    {
    }

    public void Prepare() => _ctx.ResetState();

    public void PrepareDrawPass()
    {
        _ctx.ResetMaterialState();
        if (_ctx.IsDepth)
            UseShader(_ctx.CoreShaders.DepthShader);
    }

    private void UseShader(ShaderId shaderId) => _gfxCmd.UseShader(shaderId);

    public void DrawMesh(DrawCommand cmd, int submitIndex)
    {
        if(_ctx.PrevMaterial != cmd.MaterialId)
            _buffers.ApplyDrawMaterial(cmd.MaterialId, _applyShader);
        
        _buffers.BindDrawObject(submitIndex);
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawBoundMesh(cmd.MeshId, cmd.DrawCount);
    }
}