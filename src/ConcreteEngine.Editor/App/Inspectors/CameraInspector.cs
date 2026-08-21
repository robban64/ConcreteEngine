using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Inputs;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(Camera))]
internal sealed partial class CameraInspector : Inspector<Camera>
{
    public override uint Icon => IconNames.Video;
    public override InspectorId Id => InspectorId.Camera;

    public CameraInspector()
    {
        Sections = _fields.CreateSections();
        AttachTarget(CameraManager.Instance.Camera);
    }
    
}