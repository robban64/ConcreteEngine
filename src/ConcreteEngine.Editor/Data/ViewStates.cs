using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;

namespace ConcreteEngine.Editor.Data;

internal sealed class AssetState
{
    public AssetFileSpec[] FileSpecs = [];
    public AssetKind SelectedKind;
    
    public ReadOnlySpan<AssetObject> Assets => EngineController.AssetController.GetAssetSpan(SelectedKind);

    public void ResetState()
    {
        FileSpecs = [];
    }
}

internal sealed class SceneState
{
    public GlobalContext Context = null!;
    public SceneObjectProxy? Proxy => Context.SelectedProxy;
    public SceneObjectId SelectedId => Context.SelectedId;

    public ReadOnlySpan<ISceneObject> GetSceneObjectSpan() => EngineController.SceneController.GetSceneObjectSpan();

}