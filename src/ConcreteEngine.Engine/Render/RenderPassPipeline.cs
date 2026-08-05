using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderPassPipeline
{
    private int _activePassIndex;
    private int _passCount;

    private readonly RenderPassContext _ctx;
    private readonly RenderRegistry _renderRegistry;
    private readonly RenderPassEntry[] _entries;

    internal RenderPassPipeline(DrawCommandProcessor cmd, RenderRegistry renderRegistry)
    {
        _renderRegistry = renderRegistry;
        _ctx = new RenderPassContext(cmd);
        _entries = new RenderPassEntry[16];
    }

    public int PassCount => _passCount;

    internal void ResetFrame()
    {
        RenderContext.ResetContext();
        _activePassIndex = 0;
        _ctx.Reset();
    }


    internal bool NextPass(out PassId passId, out NextPassAction action)
    {
        if ((uint)_activePassIndex >= (uint)_passCount)
        {
            passId = default;
            action = default;
            return false;
        }

        var passEntry = _entries[_activePassIndex];
        var passKey = passEntry.PassKey;

        var key = passEntry.DependsOn is { } dependsOnKey
            ? new FboKey(dependsOnKey.TagIndex, passKey.Variant)
            : new FboKey(passKey.TagIndex, passKey.Variant);

        action = NextPassAction.Run;

        if (_renderRegistry.TryGetRenderFbo(key, out var fbo))
            _ctx.AttachPass(fbo.FboId, passKey);
        else if (passEntry.PassOp == PassOp.Screen)
            _ctx.AttachScreenPass(passKey);
        else
            action = NextPassAction.Skip;

        _ctx.DequeueMutationTo(passEntry);
        _ctx.DequeuePassSources(passEntry);

        passId = passEntry.PassKey.Pass;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PassAction ApplyPass() => _entries[_activePassIndex].ApplyPass(_ctx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ApplyAfterPass() => _entries[_activePassIndex++].ApplyAfterPass(_ctx);


    public RenderPassEntry RegisterContinue<TTarget>(FboVariant variant, PassId passId, PassOp op,
        RenderPassState initial)
        where TTarget : unmanaged, IRenderTarget
    {
        var existingKey = RenderRegistry.TargetRegistry<TTarget>.PassKey(variant);
        if (existingKey.Pass == passId) Throwers.InvalidArgument(nameof(passId));
        return AddPassEntry(existingKey with { Pass = passId }, op, initial);
    }


    public RenderPassEntry Register<TTarget>(FboVariant variant, PassId passId, PassOp op, RenderPassState initial)
        where TTarget : unmanaged, IRenderTarget
    {
        var key = RenderRegistry.TargetRegistry<TTarget>.BindPassTarget(variant, passId);
        return AddPassEntry(key, op, initial);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private RenderPassEntry AddPassEntry(PassTargetKey key, PassOp op, RenderPassState initial)
    {
        foreach (var e in _entries.AsSpan(0, _passCount))
        {
            if (e.PassKey == key) Throwers.InvalidArgument("Duplicated passes");
        }

        return _entries[_passCount++] = new RenderPassEntry(key, op, initial);
    }
}