using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Core.Engine.Command;

public abstract record EngineCommandRecord(CommandScope Scope)
{
    private static int _idx;
    public int Id { get; } = ++_idx;
    public DateTime Timestamp { get; } = DateTime.Now;

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Id = ");
        builder.Append(Id);
        builder.Append(", Scope = ");
        builder.Append(EnumCache<CommandScope>.Names[(int)Scope]);
        return true;
    }
}

public sealed record AssetCommandRecord(CommandAssetAction Action, AssetId Asset, AssetKind Kind)
    : EngineCommandRecord(CommandScope.Asset);

public sealed record FboCommandRecord(CommandFboAction Action, Size2D Size) : EngineCommandRecord(CommandScope.Render);