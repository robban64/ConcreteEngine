#region

using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Renderer.Passes;

internal sealed class PassCommandQueue
{
    private readonly PriorityQueue<TextureId, PassTextureSlotKey> _sourceQueue;
    private readonly PriorityQueue<PassMutationState, PassTagKey> _mutationQueue;

    private readonly TextureId[] _textureSlots = new TextureId[RenderLimits.TextureSlots];
    private int _maxTexSlot = 0;

    internal PassCommandQueue()
    {
        _sourceQueue = new PriorityQueue<TextureId, PassTextureSlotKey>(new PassTextureSlotKeyComp());
        _mutationQueue = new PriorityQueue<PassMutationState, PassTagKey>(new PassTagKeyComp());
    }

    public void SampleTo(PassTextureSlotKey texKey, TextureId textureId)
    {
        _sourceQueue.Enqueue(textureId, texKey);
    }

    public void EnqueueMutation(PassTagKey passKey, in PassMutationState newState)
    {
        _mutationQueue.Enqueue(newState, passKey);
    }

    public void DequeueMutationTo(RenderPassEntry entry)
    {
        while (_mutationQueue.TryPeek(out _, out var k) && k.TagIndex == entry.PassKey.TagIndex)
        {
            _mutationQueue.TryDequeue(out var state, out k);
            entry.UpdateState(in state);
        }
    }

    public void DequeuePassSources(RenderPassEntry entry)
    {
        var tagIndex = entry.PassKey.TagIndex;
        _textureSlots.AsSpan().Clear();
        _maxTexSlot = 0;

        while (_sourceQueue.TryPeek(out _, out var k) && k.TagIndex == tagIndex)
        {
            _sourceQueue.TryDequeue(out var id, out k);
            _textureSlots[k.TextureSlot] = id;
            _maxTexSlot = int.Max(_maxTexSlot, k.TextureSlot);
        }
    }

/*
    public IReadOnlyList<TextureId> GetPassSources()
    {
        return _textureSlots;
    }
*/
    public ReadOnlySpan<TextureId> GetPassSources()
    {
        return _textureSlots.AsSpan(0, int.Max(_maxTexSlot, 1));
    }

    internal void Prepare()
    {
        _sourceQueue.Clear();
        _mutationQueue.Clear();
        _textureSlots.AsSpan().Clear();
        _maxTexSlot = 0;
    }
}