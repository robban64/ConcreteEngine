using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Command;
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

internal sealed class RenderCommandSurface
{
    private static VisualManager Visuals => VisualManager.Instance;
    private static EngineWindow Window => EngineWindow.Current;

    public void Apply(FboCommandRecord cmd)
    {
        switch (cmd.Action)
        {
            case CommandFboAction.ShadowSize: Visuals.Shadow.ShadowMapSize = cmd.Size.Width; break;
            case CommandFboAction.ScreenSize: Visuals.MarkPendingOutputSize(); break;
            case CommandFboAction.None: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}