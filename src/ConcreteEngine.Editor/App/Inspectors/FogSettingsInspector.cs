using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(FogSettings))]
internal sealed partial class FogSettingsInspector : Inspector<FogSettings>
{
   
    public override InspectorId Id => InspectorId.Environment;

    public override uint Icon => IconNames.CloudFog;

    public FogSettingsInspector()
    {
        Sections = _fields.CreateSections();
        AttachTarget(VisualManager.Instance.Fog);
    }

    public override void Draw()
    {
        foreach (var section in Sections) section.Draw();
    }
}