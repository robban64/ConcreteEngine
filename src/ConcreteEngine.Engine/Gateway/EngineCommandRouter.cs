using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.Core;

namespace ConcreteEngine.Engine.Gateway;

internal static class EngineCommandRouter
{
    internal static EngineCommandQueue? CommandCommandQueues { get; set; }

    public static CommandResponse AssetEndpoint(AssetCommandRecord command)
    {
        ArgumentNullException.ThrowIfNull(command);
        CommandCommandQueues?.Enqueue(command);
        return CommandResponse.Ok();
    }

    public static CommandResponse RenderEndpoint(FboCommandRecord command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Size.IsNegativeOrZero()) throw new ArgumentOutOfRangeException(nameof(command.Size));
        CommandCommandQueues?.Enqueue(command);
        return CommandResponse.Ok();
    }
}