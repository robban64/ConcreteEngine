using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(Camera))]
internal sealed partial class CameraInspector : Inspector<CameraInspector>
{
    private static Camera Target => CameraManager.Instance.Camera;

    private static void DrawRootBind() => Instance.DrawRoot();
    
    public unsafe void Draw()
    {
        AppDraw.Section("Transform"u8, &DrawRootBind);
    }

}
