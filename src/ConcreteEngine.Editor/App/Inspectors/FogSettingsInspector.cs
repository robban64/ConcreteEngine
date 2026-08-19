using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(FogSettings))]
internal sealed partial class FogSettingsInspector : Inspector<FogSettingsInspector>
{
    private static FogSettings Target => VisualManager.Instance.Fog;
    public override uint Icon => IconNames.CloudFog;

    public void Draw()
    {
        foreach (var section in Sections) section.Draw();
    }
}