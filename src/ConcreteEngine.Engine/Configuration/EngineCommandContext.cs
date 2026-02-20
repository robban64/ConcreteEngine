using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Worlds;

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
        if (cmd.Action == CommandAssetAction.Reload && cmd.Kind == AssetKind.Shader)
        {
            assetSystem.EnqueueReloadAsset(cmd);
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }
    }
}

internal sealed class RenderCommandSurface(WorldVisual visual)
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