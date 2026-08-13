using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Engine.Render.Passes;

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
        _program = new RenderPassProgram(cmd);
        _entries = new RenderPassEntry[16];
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

        var key = passEntry.DependsOn is { } dependsOnKey
            ? new FboKey(dependsOnKey.TagIndex, passKey.Variant)
            : new FboKey(passKey.TagIndex, passKey.Variant);

        var action = NextPassAction.Run;

        if (_renderRegistry.TryGetRenderFbo(key, out var fbo))
            _program.AttachPass(fbo.FboId, passKey);
        else if (passEntry.PassOp == PassOp.Screen)
            _program.AttachScreenPass(passKey);
        else
            action = NextPassAction.Skip;

        _program.DequeueMutationTo(passEntry);
        _program.DequeuePassSources(passEntry.PassKey);

        result = (passEntry.PassKey.Pass, action);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PassAction ApplyPass() => _entries[_activePassIndex].ApplyPass(_program);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ApplyAfterPass() => _entries[_activePassIndex++].ApplyAfterPass(_program);


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