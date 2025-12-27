using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Editor.Definitions;

namespace ConcreteEngine.Engine.Editor.Data;

internal interface IEngineCommandRecord
{
    EngineCommandScope Scope { get; }
    int CommandId { get; }
    long Timestamp { get; }
}

internal interface IWorldCommandRecord : IEngineCommandRecord
{
    WorldCommandAction Action { get; }
}

internal abstract class EngineCommandRecord(EngineCommandScope scope) : IEngineCommandRecord
{
    private static int _idx = 0;
    public int CommandId { get; } = ++_idx;
    public long Timestamp { get; } = TimeUtils.GetTimestamp();
    public EngineCommandScope Scope { get; init; } = scope;
}

internal sealed class AssetCommandRecord(string name, AssetCommandAction action, AssetKind kind)
    : EngineCommandRecord(EngineCommandScope.AssetCommand)
{
    public string Name { get; init; } = name;
    public AssetCommandAction Action { get; init; } = action;
    public AssetKind Kind { get; init; } = kind;
}

internal sealed class FboCommandRecord(FboCommandAction action, Size2D size)
    : EngineCommandRecord(EngineCommandScope.RenderCommand)
{
    public FboCommandAction Action { get; init; } = action;
    public Size2D Size { get; init; } = size;
}

internal sealed class EntityCommandRecord<TData>(WorldCommandAction action, int entityId, in TData data)
    : EngineCommandRecord(EngineCommandScope.WorldCommand), IWorldCommandRecord where TData : unmanaged
{
    private readonly TData _data = data;
    public ref readonly TData Data => ref _data;
    public WorldCommandAction Action { get; init; } = action;
    public int EntityId { get; init; } = entityId;
}