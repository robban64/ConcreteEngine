using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(SceneObject))]
internal sealed partial class SceneObjectInspector : Inspector<SceneObjectInspector>
{
    private static SceneObject Target => SelectionManager.Instance.SelectedSceneObject!;
    public override uint Icon => IconNames.Box;

    public void Draw()
    {
        foreach (var section in Sections) section.Draw();
    }
}