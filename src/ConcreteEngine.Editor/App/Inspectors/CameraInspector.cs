using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(Camera))]
internal static partial class CameraInspector
{
    private static Camera Target => CameraManager.Instance.Camera;

}
