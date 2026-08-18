using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(FogSettings))]
internal sealed partial class FogSettingsInspector : Inspector<FogSettingsInspector>
{
    private static FogSettings Target => VisualManager.Instance.Fog;

    public void Draw()
    {
        _sectionRoot.Draw();
    }
}