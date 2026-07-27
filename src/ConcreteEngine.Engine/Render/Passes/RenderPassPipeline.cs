using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Engine.Render.Registry;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassPipeline
{
    private int _activePassIndex;

    private readonly RenderPassCtx _ctx;
    private readonly RenderRegistry _renderRegistry;
    private readonly List<RenderPassEntry> _entries = new(8);

    internal RenderPassPipeline(GfxContext gfx, RenderRegistry renderRegistry)
    {
        _renderRegistry = renderRegistry;
        _ctx = new RenderPassCtx(gfx);
    }

    public int PassCount => _entries.Count;


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
        var key = RenderRegistry.TargetRegistry<TTarget>.BindFboPassId(variant, passId);
        foreach (var e in _entries)
        {
            if (e.PassKey.Pass == passId || e.PassKey == key) Throwers.InvalidArgument("Duplicated passes");
        }

        var entry = new RenderPassEntry(key, op, initial);
        _entries.Add(entry);
        return entry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Prepare()
    {
        _activePassIndex = 0;
        _ctx.Prepare();
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
            ? new FboTagKey(dependsOnKey.TagIndex, passKey.Variant)
            : new FboTagKey(passKey.TagIndex, passKey.Variant);

        action = NextPassAction.Run;

        if (_renderRegistry.TryGetRenderFbo(key, out var fbo))
            _ctx.AttachPass(fbo, passKey);
        else if (passEntry.PassOp == PassOp.Screen)
            _ctx.AttachScreenPass(passKey, EngineWindow.ViewportSize);
        else
            action = NextPassAction.Skip;

        _ctx.DequeueMutationTo(passEntry);
        _ctx.DequeuePassSources(passEntry);

        passId = passEntry.PassKey.Pass;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PassAction ApplyPass()
    {
        return _entries[_activePassIndex].ApplyPass(_ctx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ApplyAfterPass()
    {
        _entries[_activePassIndex].ApplyAfterPass(_ctx);
        _activePassIndex++;
    }
}