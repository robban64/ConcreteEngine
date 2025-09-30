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
    public PassTagKey TagKey { get; private set; }

    private readonly IReadOnlyDictionary<PassTagKey, RenderPassEntry> _registry;
    private readonly Dictionary<PassTagKey, List<TextureId>> _textureSlot = new(4);

    internal RenderPassCtx(RenderCommandOps cmdOps, IReadOnlyDictionary<PassTagKey, RenderPassEntry> registry)
    {
        CmdOps = cmdOps;
        _registry = registry;
    }

    public IReadOnlyList<TextureId> GetPassSources()
    {
        if (!_textureSlot.TryGetValue(TagKey, out var list))
            throw new KeyNotFoundException($"No passes were found for {TagKey} at Pass: {Pass}");

        return list;
    }

    public void SampleTo<TTag, TSlot>(PassOpKind passOp, int slot, TextureId textureId)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        Debug.Assert(slot >= 0 && slot < 16);
        var key = PassTagKey.Make<TTag, TSlot>(passOp);
        if (!_textureSlot.TryGetValue(key, out var list))
            _textureSlot[key] = list = new List<TextureId>(4);

        while (list.Count <= slot) list.Add(default);

        list[slot] = textureId;
    }

    public void MutateStatePass<TTag, TSlot>(PassOpKind passOp, in PassMutationState newState)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        var key = PassTagKey.Make<TTag, TSlot>(passOp);
        if (_registry.TryGetValue(key, out RenderPassEntry entry))
        {
            entry.UpdateState(in newState);
        }
    }

    public void MutateStatePass<TTag, TSlot>(PassOpKind passOp, RenderPassMutate mutate)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        var key = PassTagKey.Make<TTag, TSlot>(passOp);
        if (_registry.TryGetValue(key, out RenderPassEntry entry))
        {
            entry.UpdateState(mutate);
        }
    }

    internal void Prepare()
    {
        FboId = default;
        Meta = default;
        Pass = 0;
    }

    internal void AttachScreenPass(Size2D outputSize, int pass, PassTagKey tagKey)
    {
        FboId = default;
        Meta = new FrameBufferMeta(outputSize, default, default);
        Pass = pass;
        TagKey = tagKey;
    }

    internal void AttachPass(RenderFbo fbo, int pass, PassTagKey tagKey)
    {
        FboId = fbo.FboId;
        Meta = fbo.GetMeta();
        Pass = pass;
        TagKey = tagKey;
    }
}