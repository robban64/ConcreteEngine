using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Metadata.Command;

namespace ConcreteEngine.Engine.Editor;

internal static class EngineCommandRouter
{
    internal static EditorEngineQueue CommandQueues { get; set; }

    public static CommandResponse OnAssetShaderCmd(AssetCommandRecord shaderCommand)
    {
        ArgumentNullException.ThrowIfNull(shaderCommand);
        ArgumentException.ThrowIfNullOrWhiteSpace(shaderCommand.Name);
        CommandQueues.EnqueueDeferred(shaderCommand);
        return CommandResponse.Ok();
    }

    public static CommandResponse OnWorldShadowCmd(FboCommandRecord command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if(command.Size.IsNegativeOrZero()) throw new ArgumentOutOfRangeException(nameof(command.Size));
        CommandQueues.EnqueueDeferred(command);
        return CommandResponse.Ok();
    }

}