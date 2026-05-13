using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer.Passes;

internal readonly record struct PreparePassResult(int TagIndex, PassId PassId, PreparePassActionKind ActionKind);

internal sealed class RenderPassPipeline
{
    private readonly RenderFboRegistry _fboRegistry;
    private readonly List<RenderPassEntry> _entries = new(8);

    private RenderPassCtx _ctx = null!;
    private PassCommandQueue _cmdQueue = null!;

    private int _activePassIndex;

    internal RenderPassPipeline(RenderFboRegistry fboRegistry)
    {
        _fboRegistry = fboRegistry;
    }

    public int PassCount => _entries.Count;

    internal void Initialize(RenderProgramContext ctx)
    {
        _cmdQueue = new PassCommandQueue();
        _ctx = new RenderPassCtx(ctx.CommandPipeline.DrawStateOps, _cmdQueue);
    }


    public RenderPassEntry RegisterContinue<TTag>(FboVariant variant, PassId passId, PassOp op,
        RenderPassState initial)
        where TTag : class
    {
        var existingKey = PassTags<TTag>.PassKey(variant);
        InvalidOpThrower.ThrowIf(existingKey.Pass == passId);

        var newKey = existingKey with { Pass = passId };

        foreach (var e in _entries)
        {
            if (e.PassKey.Pass == passId || e.PassKey == newKey)
                throw new InvalidOperationException("Duplicated passes");
        }

        var entry = new RenderPassEntry(newKey, op, initial);
        _entries.Add(entry);
        return entry;
    }


    public RenderPassEntry Register<TTag>(FboVariant variant, PassId passId, PassOp op, RenderPassState initial)
        where TTag : class
    {
        var key = PassTags<TTag>.BindFboPassId(variant, passId);

        foreach (var e in _entries)
        {
            if (e.PassKey.Pass == passId || e.PassKey == key)
                throw new InvalidOperationException("Duplicated passes");
        }

        var entry = new RenderPassEntry(key, op, initial);
        _entries.Add(entry);
        return entry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Prepare()
    {
        _activePassIndex = 0;
        _cmdQueue.Prepare();
    }


    internal bool NextPass(out PreparePassResult result)
    {
        if ((uint)_activePassIndex >= (uint)_entries.Count)
        {
            result = default;
            return false;
        }

        var passEntry = _entries[_activePassIndex];
        var passKey = passEntry.PassKey;

        var key = passEntry.DependsOn is { } dependsOnKey
            ? new FboTagKey(dependsOnKey.TagIndex, passKey.Variant)
            : new FboTagKey(passKey.TagIndex, passKey.Variant);

        var kind = PreparePassActionKind.Run;
        if (_fboRegistry.TryGetRenderFbo(key, out var fbo))
            _ctx.AttachPass(fbo, passKey);
        else if (passEntry.PassOp == PassOp.Screen)
            _ctx.AttachScreenPass(passKey, VisualRenderContext.Instance.OutputSize);
        else
            kind = PreparePassActionKind.Skip;

        _cmdQueue.DequeueMutationTo(passEntry);
        _cmdQueue.DequeuePassSources(passEntry);

        result = new PreparePassResult(passEntry.PassKey.TagIndex, passEntry.PassKey.Pass, kind);
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