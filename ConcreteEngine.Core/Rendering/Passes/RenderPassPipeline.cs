#region

using System.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.Registry;

#endregion

namespace ConcreteEngine.Core.Rendering.Passes;

internal readonly record struct PreparePassResult(int TagIndex, PassId PassId, PreparePassActionKind ActionKind);

public sealed class RenderPassPipeline
{
    private RenderPassCtx _ctx = null!;
    private RenderRegistry _renderRegistry = null!;

    private PassCommandQueue _cmdQueue = null!;

    private readonly List<RenderPassEntry> _entries = new(8);

    private int _passIter = 0;
    private RenderPassEntry? _currentEntry = null;

    private Size2D _outputSize;

    internal RenderPassPipeline()
    {
    }

    internal void Initialize(RenderSystemContext ctx)
    {
        _renderRegistry = ctx.Registry;
        _cmdQueue = new PassCommandQueue();
        _ctx = new RenderPassCtx(ctx.CommandPipeline.DrawStateOps, _cmdQueue);
    }

    public RenderPassEntry Register<TTag>(FboVariant variant, PassId passId, PassOpKind opKind, RenderPassState initial)
        where TTag : unmanaged, IRenderPassTag
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
        if (_renderRegistry.TryGetRenderFbo(key, out var fbo))
            _ctx.AttachPass(fbo, pass.PassKey);
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

    internal PassAction ApplyPass()
    {
        Debug.Assert(_currentEntry != null);
        return _currentEntry.ApplyPass(_ctx);
    }

    internal void ApplyAfterPass()
    {
        _currentEntry!.ApplyAfterPass(_ctx);
    }
}