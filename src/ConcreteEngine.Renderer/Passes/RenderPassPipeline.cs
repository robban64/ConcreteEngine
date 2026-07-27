using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer.Passes;

internal sealed class RenderPassPipeline
{
    private readonly RenderRegistry _renderRegistry;
    private readonly List<RenderPassEntry> _entries = new(8);

    private RenderPassCtx _ctx = null!;

    private int _activePassIndex;

    internal RenderPassPipeline(RenderRegistry renderRegistry)
    {
        _renderRegistry = renderRegistry;
    }

    public int PassCount => _entries.Count;

    internal void Initialize(RenderProgramContext ctx)
    {
        _ctx = new RenderPassCtx(ctx.Gfx, ctx.CommandPipeline.UniformUploader);
    }


    public RenderPassEntry RegisterContinue<TTarget>(FboVariant variant, PassId passId, PassOp op,
        RenderPassState initial)
        where TTarget : unmanaged, IRenderTarget
    {
        var existingKey = TargetRegistry<TTarget>.PassKey(variant);

        if (existingKey.Pass == passId) Throwers.InvalidArgument(nameof(passId));

        var newKey = existingKey with { Pass = passId };

        foreach (var e in _entries)
        {
            if (e.PassKey.Pass == passId || e.PassKey == newKey)
                Throwers.InvalidArgument("Duplicated passes");
        }

        var entry = new RenderPassEntry(newKey, op, initial);
        _entries.Add(entry);
        return entry;
    }


    public RenderPassEntry Register<TTarget>(FboVariant variant, PassId passId, PassOp op, RenderPassState initial)
        where TTarget : unmanaged, IRenderTarget
    {
        var key = TargetRegistry<TTarget>.BindFboPassId(variant, passId);

        foreach (var e in _entries)
        {
            if (e.PassKey.Pass == passId || e.PassKey == key)
                Throwers.InvalidArgument("Duplicated passes");
        }

        var entry = new RenderPassEntry(key, op, initial);
        _entries.Add(entry);
        return entry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Prepare()
    {
        _activePassIndex = 0;
        _ctx.PassQueue.Prepare();
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
            _ctx.AttachScreenPass(passKey, RenderContext.Instance.OutputSize);
        else
            action = NextPassAction.Skip;

        _ctx.PassQueue.DequeueMutationTo(passEntry);
        _ctx.PassQueue.DequeuePassSources(passEntry);

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