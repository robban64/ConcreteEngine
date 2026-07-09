using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Widgets;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(Camera))]
internal static partial class CameraInspector
{
    private static Camera Target => CameraManager.Instance.Camera;
}
