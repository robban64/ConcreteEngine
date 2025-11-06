using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Editor.Definitions;

namespace ConcreteEngine.Core.Editor.Data;

internal abstract record EngineCommandRecord(EngineCommandScope Scope)
{
    private static int _idx = 0;
    public int CommandId { get; } = ++_idx;
}

internal sealed record AssetCommandRecord(string Name, AssetCommandAction Action, AssetKind Kind)
    : EngineCommandRecord(EngineCommandScope.AssetCommand);

internal sealed record FboCommandRecord(FboCommandAction Action, Size2D Size)
    : EngineCommandRecord(EngineCommandScope.RenderCommand);