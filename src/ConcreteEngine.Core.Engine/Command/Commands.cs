using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Core.Engine.Command;

public abstract record EngineCommandRecord(CommandScope Scope)
{
    private static int _idx;
    public int CmdId { get; }= ++_idx;
    public DateTime Timestamp { get; } = DateTime.Now;

    public string ToStringSlim()
    {
        var str = ToString();
        var span = str.AsSpan();
        span = span.Slice(span.IndexOf('{'));
        span.Trim();
        return span.ToString();
    }
}

public sealed record AssetCommandRecord(CommandAssetAction Action, AssetId Asset, AssetKind Kind) : EngineCommandRecord(CommandScope.Asset);
public sealed record FboCommandRecord(CommandFboAction Action, Size2D Size) : EngineCommandRecord(CommandScope.Render);