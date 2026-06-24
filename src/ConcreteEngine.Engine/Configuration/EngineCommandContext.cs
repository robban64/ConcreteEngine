using ConcreteEngine.Core.Engine;
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

    public void ApplyRender(FboCommandRecord cmd)
    {
        switch (cmd.Action)
        {
            case CommandFboAction.ShadowSize: VisualManager.Instance.Shadow.ShadowMapSize = cmd.Size.Width; break;
            case CommandFboAction.None: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}