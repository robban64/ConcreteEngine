using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(DirectionalLight))]
internal sealed partial class DirectionalLightInspector : Inspector<DirectionalLightInspector>
{
    private static DirectionalLight Target => VisualManager.Instance.Illumination;

    public unsafe void Draw()
    {
        AppDraw.Section("Directional Light"u8, &DrawRoot);
    }
}

[EditorInspector(typeof(ShadowSettings))]
internal sealed partial class ShadowSettingsInspector : Inspector<ShadowSettingsInspector>
{
    private static ShadowSettings Target => VisualManager.Instance.Shadow;

    public void Draw()
    {
        AppDraw.Section(Projection);
        AppDraw.Section(Visuals);
    }
}

[EditorInspector(typeof(EnvironmentSettings))]
internal sealed partial class EnvironmentSettingsInspector : Inspector<EnvironmentSettingsInspector>
{
    private static EnvironmentSettings Target => VisualManager.Instance.Environment;

    public unsafe void Draw()
    {
        AppDraw.CollapseSection("Ambient"u8, &DrawAmbient);
        AppDraw.CollapseSection("Fog"u8, &DrawFogSection);
    }

    private static void DrawFogSection()
    {
        AppDraw.Section(Instance.FogOptics);
        Instance.FogColor.Draw();
        AppDraw.Section(Instance.FogHeight);
    }
}

[EditorInspector(typeof(PostEffectSettings))]
internal sealed partial class PostEffectSettingsInspector : Inspector<PostEffectSettingsInspector>
{
    private static PostEffectSettings Target => VisualManager.Instance.PostEffect;

    public void Draw()
    {
        AppDraw.Section(Grade);
        AppDraw.Section(WhiteBalance);
        AppDraw.Section(Bloom);
        AppDraw.Section(ImageFx);
    }
}