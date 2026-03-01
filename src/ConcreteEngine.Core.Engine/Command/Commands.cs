using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Core.Engine.Command;

public sealed class EngineCommandPackage
{
    public readonly EngineCommandRecord Command;
    public readonly EngineCommandMeta Meta;

    public EngineCommandPackage(EngineCommandRecord command, EngineCommandMeta meta)
    {
        Command = command;
        Meta = meta;
    }

    public EngineCommandPackage(EngineCommandRecord command)
    {
        Command = command;
        Meta = new EngineCommandMeta();
    }

    public override string ToString() => $"{Meta} - {Command}";
}

public readonly struct EngineCommandMeta()
{
    private static int _idx;
    public readonly Guid Id = Guid.NewGuid();
    public readonly DateTime Timestamp = DateTime.Now;
    public readonly int GlobalId = ++_idx;

    public override string ToString() => $"[{Id}][{GlobalId}][{Timestamp}]";
}

public abstract record EngineCommandRecord(CommandScope Scope)
{
    public string ToStringSlim()
    {
        var str = ToString();
        var span = str.AsSpan();
        span = span.Slice(span.IndexOf('{'));
        span.Trim();
        return span.ToString();
    }
}

public sealed record AssetCommandRecord(CommandAssetAction Action, AssetId Asset)
    : EngineCommandRecord(CommandScope.Asset);

public sealed record FboCommandRecord(CommandFboAction Action, Size2D Size) : EngineCommandRecord(CommandScope.Render);