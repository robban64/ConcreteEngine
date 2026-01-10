using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;

namespace ConcreteEngine.Editor.Data;

internal sealed class SlotState<T> where  T :unmanaged
{
    public T State;
    public long Generation;
    public EditorSlot<T> GetView() => new(ref State, ref Generation);
}

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
    public SceneObjectProxy? Proxy;
    public ReadOnlySpan<ISceneObject> SceneObjects => EngineController.SceneController.GetSceneObjectSpan();

}