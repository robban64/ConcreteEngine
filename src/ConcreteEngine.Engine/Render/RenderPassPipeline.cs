using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderPassPipeline
{
    private int _activePassIndex;
    private int _passCount;

    private readonly RenderPassEntry[] _entries;
    private readonly RenderPassProgram _program;
    private readonly RenderRegistry _renderRegistry;

    internal RenderPassPipeline(DrawCommandProcessor cmd, RenderRegistry renderRegistry)
    {
        _renderRegistry = renderRegistry;
        _entries = new RenderPassEntry[16];
        _program = new RenderPassProgram(cmd, _entries);
    }

    internal void ResetFrame()
    {
        RenderContext.ResetContext();
        _activePassIndex = 0;
        _program.Reset();
    }


    internal bool NextPass(out (PassId Pass, NextPassAction NextAction) result)
    {
        if ((uint)_activePassIndex >= (uint)_passCount)
        {
            result = default;
            return false;
        }

        var passEntry = _entries[_activePassIndex];
        var passKey = passEntry.PassKey;

        var action = NextPassAction.Run;

        if (_renderRegistry.TryGetRenderFbo(passKey, out var fbo))
            _program.AttachPass(fbo.FboId, passKey, passEntry.State);
        else if (passEntry.PassOp == PassOp.Screen)
            _program.AttachScreenPass(passKey, passEntry.State);
        else
            action = NextPassAction.Skip;


        result = (passKey.Pass, action);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PassAction ApplyPass() => _entries[_activePassIndex].ApplyPass(_program);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ApplyAfterPass() => _entries[_activePassIndex++].ApplyAfterPass(_program);

    public RenderPassEntry Register<TTarget>(FboVariant variant, PassOp op, GfxPassState gfxState, ShaderId shaderId  = default, bool linearFilter = false)
        where TTarget : unmanaged, IRenderTarget
    {
        var key = RenderRegistry.TargetRegistry<TTarget>.BindPassTarget(variant, new PassId(_passCount));
        return AddPassEntry(key, op, gfxState, shaderId, linearFilter);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private RenderPassEntry AddPassEntry(PassTargetKey key, PassOp op, GfxPassState gfxState, ShaderId shaderId, bool linearFilter)
    {
        foreach (var e in _entries.AsSpan(0, _passCount))
        {
            if (e.PassKey == key) Throwers.InvalidArgument("Duplicated passes");
        }

        return _entries[_passCount++] = new RenderPassEntry(key, op, gfxState, shaderId, linearFilter);
    }
}