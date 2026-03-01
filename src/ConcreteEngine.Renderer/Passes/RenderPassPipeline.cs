using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer.Passes;

internal readonly record struct PreparePassResult(int TagIndex, PassId PassId, PreparePassActionKind ActionKind);

public sealed class RenderPassPipeline
{
    private readonly RenderFboRegistry _fboRegistry;
    private readonly List<RenderPassEntry> _entries = new(8);

    private PassCommandQueue _cmdQueue = null!;
    private RenderPassCtx _ctx = null!;

    private int _passIter;
    private RenderPassEntry? _currentEntry;

    private Size2D _outputSize;

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


    public RenderPassEntry RegisterContinue<TTag>(FboVariant variant, PassId passId, PassOpKind opKind,
        RenderPassState initial)
        where TTag : class
    {
        var existingKey = TagRegistry.PassKey<TTag>(variant);
        InvalidOpThrower.ThrowIf(existingKey.Pass == passId);

        var newKey = existingKey with { Pass = passId };

        foreach (var e in _entries)
        {
            if (e.PassKey.Pass == passId || e.PassKey == newKey)
                throw new InvalidOperationException("Duplicated passes");
        }

        var entry = new RenderPassEntry(newKey, opKind, initial);
        _entries.Add(entry);
        return entry;
    }


    public RenderPassEntry Register<TTag>(FboVariant variant, PassId passId, PassOpKind opKind, RenderPassState initial)
        where TTag : class
    {
        var key = TagRegistry.BindFboPassId<TTag>(variant, passId);

        foreach (var e in _entries)
        {
            if (e.PassKey.Pass == passId || e.PassKey == key)
                throw new InvalidOperationException("Duplicated passes");
        }

        var entry = new RenderPassEntry(key, opKind, initial);
        _entries.Add(entry);
        return entry;
    }

    internal void Prepare(Size2D outputSize)
    {
        _outputSize = outputSize;
        _passIter = 0;
        _cmdQueue.Prepare();
    }


    internal bool NextPass(out PreparePassResult result)
    {
        if (_passIter >= _entries.Count)
        {
            result = default;
            return false;
        }

        var pass = _entries[_passIter++];
        Debug.Assert(pass != null);

        _currentEntry = pass;

        var skipPass = false;

        var key = new FboTagKey(pass.PassKey.TagIndex, pass.PassKey.Variant);
        if (pass.DependsOn is { } dependKey)
            key = new FboTagKey(dependKey.TagIndex, pass.PassKey.Variant);

        var hasFbo = _fboRegistry.TryGetRenderFbo(key, out var fbo);

        if (hasFbo)
            _ctx.AttachPass(fbo!, pass.PassKey);
        else if (pass.PassOp == PassOpKind.Screen)
            _ctx.AttachScreenPass(pass.PassKey, _outputSize);
        else
            skipPass = true;

        _cmdQueue.DequeueMutationTo(_currentEntry);
        _cmdQueue.DequeuePassSources(_currentEntry);


        var kind = skipPass ? PreparePassActionKind.Skip : PreparePassActionKind.Run;
        result = new PreparePassResult(pass.PassKey.TagIndex, pass.PassKey.Pass, kind);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PassAction ApplyPass()
    {
        Debug.Assert(_currentEntry != null);
        return _currentEntry.ApplyPass(_ctx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ApplyAfterPass()
    {
        _currentEntry!.ApplyAfterPass(_ctx);
    }
}