using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(Camera))]
internal sealed partial class CameraInspector : Inspector<CameraInspector>
{
    private static Camera Target => CameraManager.Instance.Camera;

    public unsafe void Draw()
    {
        AppDraw.Section("Transform"u8, &DrawTransform);
        AppDraw.Section("Projection"u8, &DrawProjection);
    }

}
