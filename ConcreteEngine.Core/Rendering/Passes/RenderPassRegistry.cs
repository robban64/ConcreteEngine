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
    
    private readonly List<PassReturn> _passReturns = new(8);
    private readonly Dictionary<(RenderTargetId, int), List<TextureId>> _fsqBindings = new(4);
    
    public readonly RenderPassCtx Ctx;
    
    public IReadOnlyDictionary<(RenderTargetId, int), List<TextureId>> FsqBindings => _fsqBindings;
    public IReadOnlyList<PassReturn> PassReturns => _passReturns;
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
        _passReturns.Clear();
        foreach (var kv in _fsqBindings)
            kv.Value.Clear();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PassReturn Collect(in PassReturn before, in PassReturn after)
    {
        // AFTER -> BEFORE -> else none
        PassReturn value = default;
        if (!before.IsNone) value = before;
        if (!after.IsNone) value = after;

        if (value.Kind == NextActionKind.SampleInPass)
        {
            var key = (value.TargetId, value.PassIndex);
            if (!_fsqBindings.TryGetValue(key, out var list))
                _fsqBindings[key] = list = new List<TextureId>(4);
            
            while (list.Count <= value.Slot) list.Add(default);
            list[value.Slot] = value.SourceTexture;

        }

        if (!after.IsNone)
        {
            _passReturns.Add(after);
            return after;
        }

        if (!before.IsNone)
        {
            _passReturns.Add(before);
            return before;
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