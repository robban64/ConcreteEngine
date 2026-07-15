using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(IlluminationSettings))]
internal sealed partial class IlluminationSettingsInspector : Inspector<IlluminationSettingsInspector>
{
    private static IlluminationSettings Target => VisualManager.Instance.Illumination;
}

[EditorInspector(typeof(ShadowSettings))]
internal sealed partial class ShadowSettingsInspector : Inspector<ShadowSettingsInspector>
{
    private static ShadowSettings Target => VisualManager.Instance.Shadow;
    public unsafe void Draw()
    {
        AppDraw.Section("Shadow Projection"u8, &DrawProjection);
        AppDraw.Section("Shadow Visuals"u8, &DrawVisuals);
    }

    private static void DrawProjection() => Instance.Projection.Draw();
    private static void DrawVisuals() => Instance.Visuals.Draw();

}

[EditorInspector(typeof(EnvironmentSettings))]
internal sealed partial class EnvironmentSettingsInspector : Inspector<EnvironmentSettingsInspector>
{
    private static EnvironmentSettings Target => VisualManager.Instance.Environment;
    public unsafe void Draw()
    {
        AppDraw.Section("Fog Optics"u8, &DrawOptics);
        AppDraw.Section("Fog Height"u8, &DrawHeight);
    }

    private static void DrawHeight() => Instance.FogHeight.Draw();

    private static void DrawOptics()
    {
        Instance.FogColor.Draw();
        Instance.FogOptics.Draw();
    }

}

[EditorInspector(typeof(PostEffectSettings))]
internal sealed unsafe partial class PostEffectSettingsInspector : Inspector<PostEffectSettingsInspector>
{
    private static PostEffectSettings Target => VisualManager.Instance.PostEffect;

    public void Draw()
    {
        AppDraw.Section(Grade.Label, &DrawGrade);
        AppDraw.Section(WhiteBalance.Label, &DrawWb);
        AppDraw.Section(Bloom.Label, &DrawBloom);
        AppDraw.Section(ImageFx.Label, &DrawFx);
    }
    
    private static void DrawGrade() => Instance.Grade.Draw();
    private static void DrawWb() => Instance.WhiteBalance.Draw();
    private static void DrawBloom() => Instance.Bloom.Draw();
    private static void DrawFx() => Instance.ImageFx.Draw();

    /*
    private InputGroup G = new InputGroup("Lol", 4,
        static dst =>
        {
            var value = Target.ImageFx;
            dst[0] = value.Grain;
            dst[1] = value.Rolloff;
        },
        static src =>
        {
            Target.ImageFx = new()
            {
                Grain = (float)src[0], Rolloff =  (float)src[1]
            };
        }
    ).WithFloatInput();
    */

}