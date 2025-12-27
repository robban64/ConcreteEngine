using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Time;

namespace ConcreteEngine.Engine.Metadata.Command;


public interface IEngineCommandRecord
{
    static abstract string EngineName { get; }
}

public abstract record EngineCommandRecord(CommandScope Scope)
{
    private static int _idx;
    public int CommandId { get; } = ++_idx; // for debugging
    public Guid CommandGuid { get; } = Guid.NewGuid(); 
    public long Timestamp { get; } = TimeUtils.GetTimestamp();
    
}

public sealed record AssetCommandRecord(string Name, AssetKind Kind, CommandAssetAction Action)
    : EngineCommandRecord(CommandScope.AssetCommand)
{
}

public sealed record FboCommandRecord(CommandFboAction Action, Size2D Size)
    : EngineCommandRecord(CommandScope.RenderCommand)
{
}