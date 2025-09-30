using System.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderPassCtx
{
    public RenderCommandOps CmdOps { get; }
    public FrameBufferId FboId { get; private set; }
    public FrameBufferMeta Meta { get; private set; }
    public int Pass { get; private set; } = 0;

    private readonly IReadOnlyDictionary<(Type, int), IRenderPassEntry> _registry;
    private readonly Dictionary<(Type, int), List<TextureId>> _textureSlot = new(4);

    internal RenderPassCtx(RenderCommandOps cmdOps, IReadOnlyDictionary<(Type, int), IRenderPassEntry> registry)
    {
        CmdOps = cmdOps;
        _registry = registry;
    }

    public IReadOnlyList<TextureId> GetPassSources<TState>()
    {
        if (!_textureSlot.TryGetValue((typeof(TState), Pass), out var list))
            throw new KeyNotFoundException($"No passes were found for {typeof(TState).Name} at Pass: {Pass}");

        return list;
    }

    public void SampleTo<TState>(int pass, TextureId textureId, int slot)
    {
        Debug.Assert(slot >= 0 && slot < 16);
        if (!_textureSlot.TryGetValue((typeof(TState), pass), out var list))
            _textureSlot[(typeof(TState), pass)] = list = new List<TextureId>(4);

        while (list.Count <= slot) list.Add(default);

        list[slot] = textureId;
    }

    public void MutateStatePass<TState>(int pass, in PassMutationState newState)
        where TState : unmanaged, IRenderPassState<TState>
    {
        if (_registry.TryGetValue((typeof(TState), pass), out IRenderPassEntry entry) &&
            entry is RenderPassEntry<TState> state)
        {
            state.UpdateState(in newState);
        }
    }

    public void MutateStatePass<TState>(int pass, RenderPassMutate<TState> mutate)
        where TState : unmanaged, IRenderPassState<TState>
    {
        if (_registry.TryGetValue((typeof(TState), pass), out IRenderPassEntry entry) &&
            entry is RenderPassEntry<TState> tEntry)
        {
            tEntry.UpdateState(mutate);
        }
    }

    internal void Prepare()
    {
        FboId = default;
        Meta = default;
        Pass = 0;
    }

    internal void AttachScreenPass(Size2D outputSize, int pass)
    {
        FboId = default;
        Meta = new FrameBufferMeta(outputSize, default, default);
        Pass = pass;
    }
    
    internal void AttachPass(RenderFbo fbo, int pass)
    {
        FboId = fbo.FboId;
        Meta = fbo.GetMeta();
        Pass = pass;
    }
}