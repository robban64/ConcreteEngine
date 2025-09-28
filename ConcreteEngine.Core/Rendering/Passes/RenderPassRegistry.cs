using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;


public delegate void RenderPassMutate<TState>(in TState state) where TState : IRenderPassState;


public delegate void RenderPassOp<TState>(in RenderPassCtx ctx, in TState state) where TState : IRenderPassState;

public sealed class RenderPassCtx
{
    public RenderCommandOps CmdOps { get; }
    
    public RenderTargetId TargetId { get; private set; }
    public FrameBufferId FboId { get; private set; }
    public int Pass { get; internal set; } = 0;
    
    internal RenderPassCtx(RenderCommandOps cmdOps)
    {
        CmdOps = cmdOps;
    }

    internal void FromRenderTarget(RenderTarget target)
    {
        TargetId = target.TargetId;
        FboId = target.FboId;
    }
}

public sealed class RenderPassRegistry
{
    private readonly RenderTarget[] _renderTargets;
    private readonly List<IRenderPassEntry>[] _registry;
    private int _currentTargetId = 0;

    public readonly RenderPassCtx Ctx;


    internal RenderPassRegistry(RenderCommandOps cmdOps)
    {
        Ctx = new RenderPassCtx(cmdOps);
        _renderTargets = new RenderTarget[RenderConsts.RenderTargetCount];
        _registry = new List<IRenderPassEntry>[RenderConsts.RenderTargetCount];
        for (int i = 0; i < RenderConsts.RenderTargetCount; i++)
        {
            _registry[i] = new List<IRenderPassEntry>(4);
        }
    }

    public RenderPassEntry<TState> Register<TState>(RenderTargetId targetId, TState initial)
        where TState : IRenderPassState
    {
        var passes = _registry[(int)targetId];
        var entry = new RenderPassEntry<TState>(targetId, passes.Count, initial);
        passes.Add(entry);
        return entry;
    }

    internal bool TryGetNextPasses(out RenderTarget target, out IReadOnlyList<IRenderPassEntry> passes)
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
    }
}