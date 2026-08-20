using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;


[EditorInspector(typeof(LightingSettings))]
internal sealed partial class LightingSettingsInspector : Inspector<LightingSettingsInspector>
{
    private static LightingSettings Target => VisualManager.Instance.Lightning;
    public override uint Icon => IconNames.Sun;

    public override void Draw()
    {
        foreach (var section in Sections) section.Draw();
    }
}
