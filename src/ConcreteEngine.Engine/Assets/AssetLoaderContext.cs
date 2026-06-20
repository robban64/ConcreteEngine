using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;

public sealed class AssetLoadContext
{
    private readonly Queue<AssetRecord>[] _queues;

    public int TotalQueued { get; private set; }
    public int TotalProcessed{ get; private set; }

    public bool IsCompleted => TotalProcessed >= TotalQueued && TotalQueued > 0;

    public AssetLoadContext()
    {
        _queues = new Queue<AssetRecord>[AssetKindUtils.AssetTypeCount];
        for (int i = 0; i < _queues.Length; i++)
        {
            _queues[i] = new Queue<AssetRecord>();
        }
    }

    public Queue<AssetRecord> GetQueue(AssetKind  kind) => _queues[kind.ToIndex()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(AssetRecord record) 
    {
        _queues[record.Kind.ToIndex()].Enqueue(record);
        TotalQueued++;
    }
    

    public void MarkRecordProcessed()
    {
        TotalProcessed++;
    }
}