using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;

internal sealed class AssetLoaderContext
{
    private const int CapacityLow = 4;
    private const int CapacityHigh = 64;

    private readonly Queue<AssetRecord>[] _queues;

    public int TotalQueued { get; private set; }
    public int TotalProcessed{ get; private set; }
    
    public AssetLoaderContext(bool fullCapacity)
    {
        _queues = new Queue<AssetRecord>[AssetKindUtils.AssetTypeCount];
        for (var i = 0; i < _queues.Length; i++)
        {
            var capacity = fullCapacity ? CapacityLow : CapacityHigh;
            if (fullCapacity && i == 0) capacity = 16;
            _queues[i] = new Queue<AssetRecord>(capacity);
        }
    }

    public int GetCount(AssetKind kind) => _queues[kind.ToIndex()].Count;
    public bool IsCompleted => TotalProcessed >= TotalQueued && TotalQueued > 0;

    public Queue<AssetRecord> GetQueue(AssetKind  kind) => _queues[kind.ToIndex()];

    public bool DrainQueue<TRecord>(AssetKind kind, int drainLimit, Action<TRecord> onAction)  where TRecord : AssetRecord
    {
        int n = drainLimit;
        var queue = GetQueue(kind);
        while (queue.TryDequeue(out var record) && record is TRecord tRecord)
        {
            onAction(tRecord);
            ++TotalProcessed;
            --TotalQueued;
            if (drainLimit > 0 && --n <= 0) break;
        }

        return queue.Count == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(AssetRecord record) 
    {
        _queues[record.Kind.ToIndex()].Enqueue(record);
        TotalQueued++;
    }
    
}