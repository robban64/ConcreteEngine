using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    //private readonly Dictionary<PassTagKey, List<TextureId>> _textureSlot = new(4);

    private readonly PriorityQueue<TextureId, PassTextureSlotKey> _queue = new(new PassTagKeyNativeComparer());
    private readonly List<TextureId> _output = new();
    internal RenderPassCtx(RenderCommandOps cmdOps, IReadOnlyDictionary<PassTagKey, RenderPassEntry> registry)
    {
        CmdOps = cmdOps;
        _registry = registry;
    }

    public IReadOnlyList<TextureId> GetPassSources()
    {
        var tagIndex = RTypeRegistry.GetPassTagValue(TagKey.TagType);
        _output.Clear();
        while (_queue.TryPeek(out _, out var k) && k.TagIndex == tagIndex)
        {
            _queue.TryDequeue(out var id, out k);
            _output.Add(id);
            //Console.WriteLine($"{id} - {k}");
        }

        return _output;
    }

    public void SampleTo<TTag, TSlot>(PassOpKind passOp, int slot, TextureId textureId)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        Debug.Assert(slot >= 0 && slot < 16);
        _queue.Enqueue(textureId, PassTextureSlotKey.Make<TTag, TSlot>(passOp, (byte)slot));
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
        _queue.Clear();
        _output.Clear();
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