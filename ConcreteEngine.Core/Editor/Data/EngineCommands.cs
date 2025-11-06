using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Editor.Definitions;

namespace ConcreteEngine.Core.Editor.Data;

internal interface IEngineCommandRecord
{
    EngineCommandScope Scope { get; }
    int CommandId { get; }
    long Timestamp { get; }
}
internal abstract record EngineCommandRecord(EngineCommandScope Scope) : IEngineCommandRecord
{
    private static int _idx = 0;
    public int CommandId { get; } = ++_idx;
    public long Timestamp { get; } = TimeUtils.GetTimestamp();
}

internal sealed record AssetCommandRecord(string Name, AssetCommandAction Action, AssetKind Kind)
    : EngineCommandRecord(EngineCommandScope.AssetCommand);

internal sealed record FboCommandRecord(FboCommandAction Action, Size2D Size)
    : EngineCommandRecord(EngineCommandScope.RenderCommand);



internal interface IWorldCommandRecord : IEngineCommandRecord
{
    WorldCommandAction Action { get; }
}

internal sealed record EntityCommandRecord<TData>(WorldCommandAction Action, int EntityId, in TData Data) 
    : EngineCommandRecord(EngineCommandScope.WorldCommand), IWorldCommandRecord where TData : unmanaged
{
    private readonly TData _data = Data;
    public ref readonly TData Data => ref _data;
}