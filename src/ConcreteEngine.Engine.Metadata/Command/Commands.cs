using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Engine.Metadata.Command;

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

public abstract record EngineCommandRecord(CommandScope Scope);

public sealed record AssetCommandRecord(CommandAssetAction Action, AssetKind Kind, string Name)
    : EngineCommandRecord(CommandScope.Asset);

public sealed record RenderCommandRecord(CommandRenderAction Action, Size2D Size)
    : EngineCommandRecord(CommandScope.Render);