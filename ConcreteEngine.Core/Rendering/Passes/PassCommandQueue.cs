using System.Diagnostics;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Passes;

internal sealed class PassCommandQueue
{
    private readonly PriorityQueue<TextureId, PassTextureSlotKey> _sourceQueue;
    private readonly PriorityQueue<PassMutationState, PassTagValueKey> _cmdQueue;

    private readonly List<TextureId> _output = new(4);

    internal PassCommandQueue()
    {
        _sourceQueue = new PriorityQueue<TextureId, PassTextureSlotKey>(new PassTextureSlotKeyComp());
    }
    
    public void SampleTo(PassTextureSlotKey texKey, TextureId textureId)
    {
        _sourceQueue.Enqueue(textureId, texKey);
    }
    
    public void EnqueueMutation(PassTagValueKey passKey, in PassMutationState newState)
    {
        _cmdQueue.Enqueue(newState, passKey);
    }
    
    public IReadOnlyList<TextureId> DequeuePassSources(PassTagKey passKey)
    {
        var tagIndex = RTypeRegistry.GetPassTagValue(passKey.TagType);
        _output.Clear();
        while (_sourceQueue.TryPeek(out _, out var k) && k.TagIndex == tagIndex)
        {
            _sourceQueue.TryDequeue(out var id, out k);
            _output.Add(id);
        }

        return _output;
    }

    internal void Prepare()
    {
        _sourceQueue.Clear();
        _output.Clear();
    }
    
}