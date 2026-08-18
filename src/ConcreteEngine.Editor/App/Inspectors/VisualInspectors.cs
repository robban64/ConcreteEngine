using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(DirectionalLight))]
internal sealed partial class DirectionalLightInspector : Inspector<DirectionalLightInspector>
{
    private static DirectionalLight Target => VisualManager.Instance.Illumination;

    public void Draw()
    {
        _sectionRoot.Draw();
    }
}

[EditorInspector(typeof(ShadowSettings))]
internal sealed unsafe partial class ShadowSettingsInspector : Inspector<ShadowSettingsInspector>
{
    private static ShadowSettings Target => VisualManager.Instance.Shadow;

    public   void Draw()
    {
        _sectionProjection.Draw();
        _sectionVisuals.Draw();
    }
}

[EditorInspector(typeof(EnvironmentSettings))]
internal sealed unsafe partial class EnvironmentSettingsInspector : Inspector<EnvironmentSettingsInspector>
{
    private static EnvironmentSettings Target => VisualManager.Instance.Environment;

    public void Draw()
    {
        _sectionAmbient.Draw();
        _sectionFog.Draw();
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
internal sealed partial class PostEffectSettingsInspector : Inspector<PostEffectSettingsInspector>
{
    private static PostEffectSettings Target => VisualManager.Instance.PostEffect;

    public void Draw()
    {
        _sectionGrade.Draw();
        _sectionWhiteBalance.Draw();
        _sectionBloom.Draw();
        _sectionImageFx.Draw();
    }
}