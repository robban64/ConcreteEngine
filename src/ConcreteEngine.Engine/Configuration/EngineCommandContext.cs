using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Engine.Assets;

namespace ConcreteEngine.Engine.Configuration;

internal sealed class EngineCommandContext(AssetSystem assetSystem)
{
    public void ApplyAsset(AssetCommandRecord cmd)
    {
        switch (cmd.Action)
        {
            case CommandAssetAction.Reload:
                assetSystem.EnqueueReloadAsset(cmd);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}