using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(IlluminationSettings))]
internal static partial class IlluminationSettingsInspector
{
    private static IlluminationSettings Target => VisualManager.Instance.Illumination;
}

[EditorInspector(typeof(ShadowSettings))]
internal static partial class ShadowSettingsInspector
{
    private static ShadowSettings Target => VisualManager.Instance.Shadow;
}

[EditorInspector(typeof(EnvironmentSettings))]
internal static partial class EnvironmentSettingsInspector
{
    private static EnvironmentSettings Target => VisualManager.Instance.Environment;
}

[EditorInspector(typeof(PostEffectSettings))]
internal static partial class PostEffectSettingsInspector
{
    private static PostEffectSettings Target => VisualManager.Instance.PostEffect;
}