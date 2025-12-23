using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Renderer.Passes;

internal sealed class PassCommandQueue
{
    private readonly PriorityQueue<TextureId, PassTextureSlotKey> _sourceQueue;
    private readonly PriorityQueue<PassMutationState, PassTagKey> _mutationQueue;

    private readonly TextureId[] _textureSlots = new TextureId[RenderLimits.TextureSlots];
    private int _maxTexSlot = 0;

    internal PassCommandQueue()
    {
        _sourceQueue = new PriorityQueue<TextureId, PassTextureSlotKey>(4, new PassTextureSlotKeyComp());
        _mutationQueue = new PriorityQueue<PassMutationState, PassTagKey>(4, new PassTagKeyComp());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SampleTo(PassTextureSlotKey texKey, TextureId textureId)
    {
        _sourceQueue.Enqueue(textureId, texKey);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnqueueMutation(PassTagKey passKey, in PassMutationState newState)
    {
        _mutationQueue.Enqueue(newState, passKey);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DequeueMutationTo(RenderPassEntry entry)
    {
        while (_mutationQueue.TryPeek(out _, out var k) && k.TagIndex == entry.PassKey.TagIndex)
        {
            _mutationQueue.TryDequeue(out var state, out k);
            entry.UpdateState(in state);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DequeuePassSources(RenderPassEntry entry)
    {
        var tagIndex = entry.PassKey.TagIndex;
        var slots = _textureSlots.AsSpan();
        slots.Clear();
        _maxTexSlot = 0;

        while (_sourceQueue.TryPeek(out _, out var k) && k.TagIndex == tagIndex)
        {
            _sourceQueue.TryDequeue(out var id, out k);
            slots[k.TextureSlot] = id;
            _maxTexSlot = int.Max(_maxTexSlot, k.TextureSlot);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<TextureId> GetPassSources() => _textureSlots.AsSpan(0, int.Max(_maxTexSlot, 1));

    internal void Prepare()
    {
        _sourceQueue.Clear();
        _mutationQueue.Clear();
        _textureSlots.AsSpan().Clear();
        _maxTexSlot = 0;
    }
}