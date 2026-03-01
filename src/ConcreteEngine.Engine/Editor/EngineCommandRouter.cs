using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Engine.Editor;

internal static class EngineCommandRouter
{
    internal static EngineCommandQueue? CommandCommandQueues { get; set; }

    public static CommandResponse AssetEndpoint(AssetCommandRecord command, EngineCommandMeta meta)
    {
        ArgumentNullException.ThrowIfNull(command);
        CommandCommandQueues?.EnqueueDeferred(new EngineCommandPackage(command, meta));
        return CommandResponse.Ok();
    }

    public static CommandResponse RenderEndpoint(FboCommandRecord command, EngineCommandMeta meta)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Size.IsNegativeOrZero()) throw new ArgumentOutOfRangeException(nameof(command.Size));
        CommandCommandQueues?.EnqueueDeferred(new EngineCommandPackage(command, meta));
        return CommandResponse.Ok();
    }
}