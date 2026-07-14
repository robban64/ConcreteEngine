using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(Camera))]
internal sealed partial class CameraInspector : Inspector<CameraInspector>
{
    private static Camera Target => CameraManager.Instance.Camera;

    private static void DrawRootBind() => Instance.DrawRoot();
    private static void DrawProjectionBind() => Instance.DrawProjection();

    private static AvgFrameTimer avg1;

    public unsafe void Draw()
    {
        avg1.BeginSample();
        AppDraw.Section("Transform"u8, &DrawRootBind);
        AppDraw.Section("Projection"u8, &DrawProjectionBind);
        if (avg1.EndSample() > 40) avg1.ResetAndPrint();

    }

}
