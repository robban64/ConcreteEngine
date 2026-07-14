using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(SceneObject))]
internal sealed partial class SceneObjectInspector : Inspector<SceneObjectInspector>
{
    private static SceneObject Target => SelectionManager.Instance.SelectedSceneObject;

    public void Draw()
    {
        DrawCollapseSection("Transform"u8, static () => Instance.DrawTransform());
    }
}
