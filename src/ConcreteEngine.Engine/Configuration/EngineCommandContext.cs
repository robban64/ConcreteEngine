using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Assets;

namespace ConcreteEngine.Engine.Configuration;

internal sealed class EngineCommandContext
{
    public required RenderCommandSurface Renderer;
    public required AssetCommandSurface Assets;
}

internal sealed class AssetCommandSurface(AssetSystem assetSystem)
{
    public void Apply(AssetCommandRecord cmd)
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

internal sealed class RenderCommandSurface(VisualEnvironment visual)
{
    public void Apply(FboCommandRecord cmd)
    {
        switch (cmd.Action)
        {
            case CommandFboAction.ShadowSize: visual.SetShadowSize(cmd.Size.Width); break;
            case CommandFboAction.RecreateScreenDependentFbo: visual.SetScreenFboSize(cmd.Size); break;
            case CommandFboAction.None: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}