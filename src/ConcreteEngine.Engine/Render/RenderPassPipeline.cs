using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderPassPipeline
{
    private int _activePassIndex;

    private readonly RenderPassContext _ctx;
    private readonly RenderRegistry _renderRegistry;
    private readonly List<RenderPassEntry> _entries = new(8);

    internal RenderPassPipeline(GfxContext gfx, DrawCommandProcessor cmd, RenderRegistry renderRegistry)
    {
        _renderRegistry = renderRegistry;
        _ctx = new RenderPassContext(gfx, cmd);
    }

    public int PassCount => _entries.Count;

    internal void ResetFrame()
    {
        RenderContext.ResetPassMode();
        _activePassIndex = 0;
        _ctx.Reset();
    }

    internal bool NextPass(out PassId passId, out NextPassAction action)
    {
        if ((uint)_activePassIndex >= (uint)_entries.Count)
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

        var newKey = existingKey with { Pass = passId };

        foreach (var e in _entries)
        {
            if (e.PassKey.Pass == passId || e.PassKey == newKey) Throwers.InvalidArgument("Duplicated passes");
        }

        var entry = new RenderPassEntry(newKey, op, initial);
        _entries.Add(entry);
        return entry;
    }


    public RenderPassEntry Register<TTarget>(FboVariant variant, PassId passId, PassOp op, RenderPassState initial)
        where TTarget : unmanaged, IRenderTarget
    {
        var key = RenderRegistry.TargetRegistry<TTarget>.BindPassTarget(variant, passId);
        foreach (var e in _entries)
        {
            if (e.PassKey.Pass == passId || e.PassKey == key) Throwers.InvalidArgument("Duplicated passes");
        }

        var entry = new RenderPassEntry(key, op, initial);
        _entries.Add(entry);
        return entry;
    }
}