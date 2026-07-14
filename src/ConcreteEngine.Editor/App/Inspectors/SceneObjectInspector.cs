using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(SceneObject))]
internal sealed partial class SceneObjectInspector : Inspector<SceneObjectInspector>
{
    private static SceneObject Target => SelectionManager.Instance.SelectedSceneObject;

    private static void DrawTransformBind() => Instance.DrawTransform();
    
    public unsafe void Draw()
    {
        AppDraw.CollapseSection("Transform"u8, &DrawTransformBind);
    }
}
