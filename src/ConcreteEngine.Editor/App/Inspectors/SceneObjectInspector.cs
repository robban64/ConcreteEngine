using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Provider;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(SceneObject))]
internal static partial class SceneObjectInspector
{
    private static SceneObject Target => SelectionManager.Instance.SelectedSceneObject.SceneObject;
}