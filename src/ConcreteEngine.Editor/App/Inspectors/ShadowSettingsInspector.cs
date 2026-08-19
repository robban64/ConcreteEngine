using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(ShadowSettings))]
internal sealed partial class ShadowSettingsInspector : Inspector<ShadowSettingsInspector>
{
    private static ShadowSettings Target => VisualManager.Instance.Shadow;

    public void Draw()
    {
        _sectionRoot.Draw();
        _sectionShadowProjection.Draw();
    }
}