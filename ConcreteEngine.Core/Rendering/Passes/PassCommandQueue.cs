using System.Diagnostics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Passes;

internal sealed class PassCommandQueue
{
    private readonly PriorityQueue<TextureId, PassTextureSlotKey> _sourceQueue;
    private readonly PriorityQueue<PassMutationState, PassTagKey> _mutationQueue;

    private readonly List<TextureId> _textureSlots = new(4);

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
        while (_mutationQueue.TryPeek(out _, out var k) && k.TagIndex == entry.TagKey.TagIndex)
        {
            _mutationQueue.TryDequeue(out var state, out k);
            entry.UpdateState(in state);
        }
    }

    public void DequeuePassSources(RenderPassEntry entry)
    {
        var tagIndex = entry.TagKey.TagIndex;
        _textureSlots.Clear();
        while (_sourceQueue.TryPeek(out _, out var k) && k.TagIndex == tagIndex)
        {
            _sourceQueue.TryDequeue(out var id, out k);
            _textureSlots.Add(id);
        }
    }
    
    public IReadOnlyList<TextureId> GetPassSources()
    {
        return _textureSlots;
    }

    public ReadOnlySpan<TextureId> GetPassSourcesSpan()
    {
        return CollectionsMarshal.AsSpan(_textureSlots);
    }

    internal void Prepare()
    {
        _sourceQueue.Clear();
        _mutationQueue.Clear();
        _textureSlots.Clear();
    }
}