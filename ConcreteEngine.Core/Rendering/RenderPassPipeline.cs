using System.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Rendering.Passes;

namespace ConcreteEngine.Core.Rendering;

internal enum PreparePassResult
{
    Done,
    Run,
    Skip
}

internal readonly record struct NextPassResult(PassId PassId, int PassTagIdx, bool SkipPass);

public sealed class RenderPassPipeline
{
    private readonly RenderPassCtx _ctx;

    private readonly RenderRegistry _renderRegistry;
    private readonly PipelineStateOps _cmdOps;

    private readonly List<RenderPassEntry> _entries;

    private readonly PassCommandQueue _cmdQueue;

    private int _passIter = 0;
    private RenderPassEntry? _currentEntry = null;

    private Size2D _outputSize;

    internal RenderPassPipeline(PipelineStateOps cmdOps, RenderRegistry renderRegistry)
    {
        _cmdOps = cmdOps;
        _renderRegistry = renderRegistry;
        _entries = new List<RenderPassEntry>(8);
        _cmdQueue = new PassCommandQueue();
        _ctx = new RenderPassCtx(_cmdOps, _cmdQueue);
    }

    public RenderPassEntry Register<TTag, TSlot>(PassOpKind opKind, int passTagIdx, RenderPassState initial)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        var key = PassTagKey.Make<TTag, TSlot>(opKind);
        foreach (var e in _entries)
        {
            if (e.TagKey == key && e.PassTagIdx == passTagIdx)
                throw new InvalidOperationException("Duplicated passes");
        }

        var entry = new RenderPassEntry(new PassId(_entries.Count), key, passTagIdx, initial);
        _entries.Add(entry);
        return entry;
    }

    internal void Prepare(Size2D outputSize)
    {
        _outputSize = outputSize;
        _passIter = 0;
        _cmdQueue.Prepare();
    }

    internal bool NextPass(out NextPassResult result)
    {
        if (_passIter >= _entries.Count)
        {
            result = default;
            return false;
        }


        var pass = _entries[_passIter++];
        Debug.Assert(pass != null);

        _currentEntry = pass;

        bool skipPass = false;
        if (_renderRegistry.TryGetRenderFbo(pass.TagKey, out var fbo))
            _ctx.AttachPass(fbo, pass.PassTagIdx, pass.TagKey);
        else if (pass.TagKey.PassOp == PassOpKind.Screen)
            _ctx.AttachScreenPass(pass.PassTagIdx, pass.TagKey, _outputSize);
        else
            skipPass = true;

        _cmdQueue.DequeueMutationTo(_currentEntry);
        _cmdQueue.DequeuePassSources(_currentEntry);
        //start

        result = new NextPassResult(pass.PassId, pass.PassTagIdx, skipPass);
        return true;
    }

    internal ApplyPassReturn ApplyPass()
    {
        Debug.Assert(_currentEntry != null);
        return _currentEntry.ApplyPass(_ctx);
    }

    internal void ApplyAfterPass()
    {
        _currentEntry!.ApplyAfterPass(_ctx);
    }
}