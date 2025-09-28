using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public delegate void RenderPassOp<TState>(in RenderPassCtx ctx, in TState state) where TState : class, IRenderPassState;

public delegate void RenderPassMutate<TState>(in TState state) where TState : class, IRenderPassState;

public interface IRenderOpsSink
{
    void ClearColor(in Color4 c);
    void SetBlend(BlendMode mode);
    void GenerateMips(TextureId tex);
}

public sealed class RenderPassCtx
{
    public readonly IRenderOpsSink Ops;
    public int Pass { get; internal set; } = 0;

    public RenderPassCtx(IRenderOpsSink ops)
    {
        Ops = ops;
    }
}

public sealed class RenderPassRegistry
{
    private readonly List<IRenderPassEntry>[] _registry;
    private readonly RenderPassCtx _ctx;
    private int _currentTargetId = 0;


    internal RenderPassRegistry(IRenderOpsSink opsSink)
    {
        _ctx = new RenderPassCtx(opsSink);
        _registry = new List<IRenderPassEntry>[RenderConsts.RenderTargetCount];
        for (int i = 0; i < RenderConsts.RenderTargetCount; i++)
        {
            _registry[i] = new List<IRenderPassEntry>(4);
        }
    }

    public RenderPassEntry<TState> Register<TState>(RenderTargetId targetId, TState initial) where TState : class, IRenderPassState
    {
        var passes = _registry[(int)targetId];
        var entry = new RenderPassEntry<TState>( targetId, passes.Count, initial);
        passes.Add(entry);
        return entry;
    }
    
    internal bool TryGetNextPasses(out RenderTargetId targetId, out IReadOnlyList<IRenderPassEntry> passes)
    {
        if (_currentTargetId >= RenderConsts.RenderTargetCount)
        {
            _currentTargetId = 0;
            targetId = (RenderTargetId)_currentTargetId;
            passes = _registry[(int)targetId];
            return false;
        }

        targetId = (RenderTargetId)_currentTargetId;
        passes = _registry[_currentTargetId];
        _currentTargetId++;
        return true;
    }
}