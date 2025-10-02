using System.Diagnostics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Passes;

internal sealed class PassCommandQueue
{
    private readonly PriorityQueue<TextureId, PassTextureSlotKey> _sourceQueue;
    private readonly PriorityQueue<PassMutationState, PassTagValueKey> _mutationQueue;

    private readonly List<TextureId> _textureSlots = new(4);

    internal PassCommandQueue()
    {
        _sourceQueue = new PriorityQueue<TextureId, PassTextureSlotKey>(new PassTextureSlotKeyComp());
        _mutationQueue = new PriorityQueue<PassMutationState, PassTagValueKey>(new PassTagValueKeyComp());
    }

    public void SampleTo(PassTextureSlotKey texKey, TextureId textureId)
    {
        _sourceQueue.Enqueue(textureId, texKey);
    }

    public void EnqueueMutation(PassTagValueKey passKey, in PassMutationState newState)
    {
        _mutationQueue.Enqueue(newState, passKey);
    }

    public void DequeueMutationTo(in RenderPassEntry entry)
    {
        while (_mutationQueue.TryPeek(out _, out var k) && k.TagIndex == entry.TagValueKey.TagIndex)
        {
            _mutationQueue.TryDequeue(out var state, out k);
            entry.UpdateState(in state);
        }
    }

    public void DequeuePassSources(in RenderPassEntry entry)
    {
        var tagIndex = entry.TagValueKey.TagIndex;
        _textureSlots.Clear();
        while (_sourceQueue.TryPeek(out _, out var k) && k.TagIndex == tagIndex)
        {
            _sourceQueue.TryDequeue(out var id, out k);
            _textureSlots.Add(id);
        }
    }

    public ReadOnlySpan<TextureId> GetPassSources()
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