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
}