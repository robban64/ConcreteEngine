using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(Camera))]
internal sealed partial class CameraInspector : Inspector<Camera>
{
    public override uint Icon => IconNames.Video;
    public override InspectorId Id => InspectorId.Camera;

    public CameraInspector()
    {
        _fields.SectionRoot.SetFetchRateHigh();

        Sections = _fields.CreateSections();
        AttachTarget(CameraManager.Instance.Camera);
    }
}