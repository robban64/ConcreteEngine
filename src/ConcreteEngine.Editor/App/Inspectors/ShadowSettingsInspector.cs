using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(ShadowSettings))]
internal sealed partial class ShadowSettingsInspector : Inspector<ShadowSettings>
{
    public override uint Icon => IconNames.SunDim;
    public override InspectorId Id => InspectorId.Lighting;

    public ShadowSettingsInspector()
    {
        Sections = _fields.CreateSections();
        AttachTarget(VisualManager.Instance.Shadow);
    }

    public override void Draw()
    {
        foreach (var section in Sections) section.Draw();
    }
}