using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(Camera))]
internal sealed partial class CameraInspector : Inspector<CameraInspector>
{
    private static Camera Target => CameraManager.Instance.Camera;

    public void Draw()
    {
        DrawSection("Transform"u8, static () => Instance.DrawRoot());
    }

}
