using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public delegate void RenderPassMutate<TState>(in TState state)
    where TState : IRenderPassState;

public delegate PassReturn RenderPassOp<TState>(in RenderPassCtx ctx, in TState state)
    where TState : IRenderPassState;

public sealed class RenderPassRegistry
{
    private readonly List<IRenderPassEntry> _registry;
    private int _currentTargetId = 0;

    public readonly RenderPassCtx Ctx;

    private readonly List<NextAction> _frameIntents = new(8);
    public IReadOnlyList<NextAction> FrameIntents => _frameIntents;
    
    public IReadOnlyList<IRenderPassEntry> RenderPasses => _registry;

    internal RenderPassRegistry(RenderCommandOps cmdOps)
    {
        Ctx = new RenderPassCtx(cmdOps);
        _registry = new List<IRenderPassEntry>(8);
    }

    public RenderPassEntry<TState> Register<TState>(RenderTargetId targetId, TState initial)
        where TState : IRenderPassState
    {
        var entry = new RenderPassEntry<TState>(targetId, _registry.Count, initial);
        _registry.Add(entry);
        return entry;
    }

    public void ResetFrame()
    {
        _frameIntents.Clear();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PassReturn Collect(in PassReturn before, in PassReturn after)
    {
        // AFTER -> BEFORE -> else none
        if (after is { HasValue: true, Action.IsNone: false })
        {
            _frameIntents.Add(after.Action);
            return after.Action;
        }

        if (before is { HasValue: true, Action.IsNone: false })
        {
            _frameIntents.Add(before.Action);
            return before.Action;
        }

        return default;
    }
    
        

    /*
    internal bool TryGetNextPasses(out IReadOnlyList<IRenderPassEntry> passes)
    {
        while (_currentTargetId < _registry.Length)
        {
            var targetId = (RenderTargetId)_currentTargetId;
            target = _renderTargets[_currentTargetId];
            var list = _registry[_currentTargetId++];
            if (list.Count > 0)
            {
                Ctx.FromRenderTarget(target);
                Ctx.Pass = list.Count - 1;

                passes = list;
                return true;
            }
        }

        target = null!;
        passes = Array.Empty<IRenderPassEntry>();
        _currentTargetId = 0;
        return false;
    }*/
}