using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;



public sealed class RenderPassRegistry
{
    private readonly List<IRenderPassEntry> _entries;
    private readonly Dictionary<(Type, int), IRenderPassEntry> _registry;

    public readonly RenderPassCtx Ctx;
    
    public IReadOnlyList<IRenderPassEntry> RenderPasses => _entries;


    internal RenderPassRegistry(RenderCommandOps cmdOps)
    {
        _entries = new List<IRenderPassEntry>(8);
        _registry = new Dictionary<(Type, int), IRenderPassEntry>(8);
        Ctx = new RenderPassCtx(cmdOps, _registry);
    }

    public RenderPassEntry<TState> Register<TState>(RenderTargetId targetId, int pass, TState initial)
        where TState : unmanaged, IRenderPassState<TState>
    {
        var entry = new RenderPassEntry<TState>(targetId, pass, initial);
        _registry.Add((typeof(TState), pass), entry);
        _entries.Add(entry);
        return entry;
    }

    public void ResetFrame()
    {
        Ctx.Prepare();
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