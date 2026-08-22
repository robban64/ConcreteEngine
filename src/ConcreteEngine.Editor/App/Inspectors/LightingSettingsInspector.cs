using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;


[EditorInspector(typeof(LightingSettings))]
internal sealed partial class LightingSettingsInspector : Inspector<LightingSettings>
{
    public override uint Icon => IconNames.Sun;
    public override InspectorId Id => InspectorId.Lighting;

    public LightingSettingsInspector()
    {
        _fields.SectionShadow.SetFetchRateLow();
        Sections = _fields.CreateSections();
        AttachTarget(VisualManager.Instance.Lightning);
    }
    
}
