using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(ShadowSettings))]
internal sealed partial class ShadowSettingsInspector : Inspector<ShadowSettingsInspector>
{
    private static ShadowSettings Target => VisualManager.Instance.Shadow;
    public override uint Icon => IconNames.SunDim;

    public override void Draw()
    {
        foreach (var section in Sections) section.Draw();
    }
}