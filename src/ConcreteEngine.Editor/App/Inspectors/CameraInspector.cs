using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Inputs;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(Camera))]
internal sealed partial class CameraInspector : Inspector<CameraInspector>
{
    private static Camera Target => CameraManager.Instance.Camera;

    public void Draw()
    {
        _sectionRoot.Draw();
    }

}