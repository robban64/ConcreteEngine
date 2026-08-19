using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;


[EditorInspector(typeof(LightingSettings))]
internal sealed partial class LightingSettingsInspector : Inspector<LightingSettingsInspector>
{
    private static LightingSettings Target => VisualManager.Instance.Lightning;

    public void Draw()
    {
        _sectionSun.Draw();
        _sectionAmbient.Draw();
    }
}
