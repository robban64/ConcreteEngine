using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Metadata.Command;

namespace ConcreteEngine.Engine.Editor;

internal static class EngineCommandRouter
{
    internal static EngineCommandQueue CommandCommandQueues { get; set; }

    public static CommandResponse OnAssetShaderCmd(AssetCommandRecord shaderCommand)
    {
        ArgumentNullException.ThrowIfNull(shaderCommand);
        ArgumentException.ThrowIfNullOrWhiteSpace(shaderCommand.Name);
        CommandCommandQueues.EnqueueDeferred(shaderCommand);
        return CommandResponse.Ok();
    }

    public static CommandResponse OnWorldShadowCmd(RenderCommandRecord command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Size.IsNegativeOrZero()) throw new ArgumentOutOfRangeException(nameof(command.Size));
        CommandCommandQueues.EnqueueDeferred(command);
        return CommandResponse.Ok();
    }
}