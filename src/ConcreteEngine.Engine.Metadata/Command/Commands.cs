using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Time;

namespace ConcreteEngine.Engine.Metadata.Command;

public sealed class EngineCommandPackage
{
    
}

public readonly record struct EngineCommandMeta
{

}
public abstract record EngineCommandRecord(CommandScope Scope)
{
    private static int _idx;
    public int CommandId { get; } = ++_idx; // for debugging
    public Guid CommandGuid { get; } = Guid.NewGuid();
    public long Timestamp { get; } = TimeUtils.GetTimestamp();
}

public sealed record AssetCommandRecord(CommandAssetAction Action, AssetKind Kind, string Name)
    : EngineCommandRecord(CommandScope.Asset);

public sealed record RenderCommandRecord(CommandRenderAction Action, Size2D Size)
    : EngineCommandRecord(CommandScope.Render);