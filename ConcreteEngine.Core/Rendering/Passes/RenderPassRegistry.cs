using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Gfx;

namespace ConcreteEngine.Core.Rendering.Passes;

public sealed class RenderPassRegistry
{
    private int _passIdx = 0;
    private readonly List<RenderPassEntry> _entries;
    private readonly Dictionary<PassTagKey, RenderPassEntry> _registry;

    public readonly RenderPassCtx Ctx;

    public IReadOnlyList<RenderPassEntry> RenderPasses => _entries;


    internal RenderPassRegistry(RenderCommandOps cmdOps)
    {
        _entries = new List<RenderPassEntry>(8);
        _registry = new Dictionary<PassTagKey, RenderPassEntry>(8);
        Ctx = new RenderPassCtx(cmdOps, _registry);
    }

    public RenderPassEntry Register<TTag, TSlot>(RenderTargetId targetId, PassOpKind opKind, int pass,
        RenderPassState initial)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        var key = PassTagKey.Make<TTag, TSlot>(opKind);

        var entry = new RenderPassEntry(targetId, key, pass, initial);
        _registry.Add(key, entry);
        _entries.Add(entry);
        return entry;
    }

    public void Prepare()
    {
        _passIdx = 0;
        Ctx.Prepare();
    }

    internal void TryGetNextPasses()
    {
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