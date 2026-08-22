using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(EnvironmentSettings))]
internal sealed partial class EnvironmentSettingsInspector : Inspector<EnvironmentSettings>
{
    public override InspectorId Id => InspectorId.Environment;

    public override uint Icon => IconNames.CloudFog;

    public EnvironmentSettingsInspector()
    {
        Sections = _fields.CreateSections();
        ApplySectionLowFetchRate();
        AttachTarget(VisualManager.Instance.Environment);
    }

}