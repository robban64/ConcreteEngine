using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Metadata.Command;

namespace ConcreteEngine.Engine.Editor;

internal static class EngineCommandRouter
{
    internal static EngineCommandQueue? CommandCommandQueues { get; set; }
    
    public static CommandResponse AssetEndpoint(AssetCommandRecord command, EngineCommandMeta meta)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        CommandCommandQueues?.EnqueueDeferred(new EngineCommandPackage(command, meta));
        return CommandResponse.Ok();
    }

    public static CommandResponse RenderEndpoint(RenderCommandRecord command, EngineCommandMeta meta)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Size.IsNegativeOrZero()) throw new ArgumentOutOfRangeException(nameof(command.Size));
        CommandCommandQueues?.EnqueueDeferred(new EngineCommandPackage(command, meta));
        return CommandResponse.Ok();
    }
}