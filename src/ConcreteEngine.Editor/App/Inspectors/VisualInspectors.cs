using ConcreteEngine.Core.Engine.Graphics.Visuals;
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
internal sealed unsafe partial class ShadowSettingsInspector : Inspector<ShadowSettingsInspector>
{
    private static ShadowSettings Target => VisualManager.Instance.Shadow;

    public void Draw()
    {
        AppDraw.Section("Projection"u8, &DrawProjection);
        AppDraw.Section("Visuals"u8, &DrawVisuals);
    }
}

[EditorInspector(typeof(EnvironmentSettings))]
internal sealed unsafe partial class EnvironmentSettingsInspector : Inspector<EnvironmentSettingsInspector>
{
    private static EnvironmentSettings Target => VisualManager.Instance.Environment;

    public void Draw()
    {
        AppDraw.CollapseSection("Ambient"u8, &DrawAmbient);
        AppDraw.CollapseSection("Fog"u8, &DrawFogSection);
    }

    private static void DrawFogSection()
    {
        AppDraw.Section("FogOptics"u8, &DrawFogOptics);
        DrawFog();
        AppDraw.Section("FogHeight"u8, &DrawFogHeight);
    }
}

[EditorInspector(typeof(PostEffectSettings))]
internal sealed unsafe partial class PostEffectSettingsInspector : Inspector<PostEffectSettingsInspector>
{
    private static PostEffectSettings Target => VisualManager.Instance.PostEffect;

    public void Draw()
    {
        AppDraw.Section("Grade"u8, &DrawGrade);
        AppDraw.Section("WhiteBalance"u8, &DrawWhiteBalance);
        AppDraw.Section("Bloom"u8, &DrawBloom);
        AppDraw.Section("ImageFx"u8, &DrawImageFx);
    }
}