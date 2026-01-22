using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class EditorEventHandler(StateContext ctx, EngineController controller)
{
    private readonly WorldController _worldController = controller.WorldController;

    public void OnCameraCommit(WorldEvent evt) => _worldController.CommitCamera(evt.CameraState);
    public void OnVisualCommit(VisualDataEvent evt) => _worldController.CommitVisualParams(evt.State);

    public void OnSelectSceneObject(SceneObjectEvent evt)
    {
        if (ctx.Selection.SelectedSceneId == evt.SceneObject) return;
        if (!evt.SceneObject.IsValid())
        {
            ctx.Selection.DeSelectSceneObject();
            ctx.EmitTransition(TransitionMessage.PopRight());
        }

        ctx.Selection.SelectSceneObject(evt.SceneObject);
        ctx.EmitTransition(TransitionMessage.PushRight(PanelId.SceneProperty));
    }

    public void OnSelectAsset(AssetEvent evt)
    {
        if (ctx.Selection.SelectedAssetId == evt.Asset) return;

        if (!evt.Asset.IsValid())
        {
            ctx.Selection.DeselectAsset();
            ctx.EmitTransition(TransitionMessage.PopRight());
            return;
        }

        ctx.Selection.SelectAsset(evt.Asset);
        ctx.EmitTransition(TransitionMessage.PushRight(PanelId.AssetProperty));
    }

    public static void OnReloadAsset(AssetReloadEvent evt)
    {
        ArgumentException.ThrowIfNullOrEmpty(evt.Name);
        var cmd = new AssetCommandRecord(CommandAssetAction.Reload, AssetKind.Shader, evt.Name);
        CommandDispatcher.InvokeEditorCommand(cmd);
    }

    public static void OnGraphicsSettings(GraphicsSettingsEvent evt)
    {
        if (evt.ShadowSize is not { } shadowSize) throw new ArgumentNullException(nameof(evt.ShadowSize));

        var payload = new FboCommandRecord(CommandFboAction.ShadowSize, new Size2D(shadowSize));
        CommandDispatcher.InvokeEditorCommand(payload);
    }

}