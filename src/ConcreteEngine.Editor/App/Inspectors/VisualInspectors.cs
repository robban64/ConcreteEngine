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
}

[EditorInspector(typeof(EnvironmentSettings))]
internal sealed partial class EnvironmentSettingsInspector : Inspector<EnvironmentSettingsInspector>
{
    private static EnvironmentSettings Target => VisualManager.Instance.Environment;
}

[EditorInspector(typeof(PostEffectSettings))]
internal sealed unsafe partial class PostEffectSettingsInspector : Inspector<PostEffectSettingsInspector>
{
    private static PostEffectSettings Target => VisualManager.Instance.PostEffect;
    private static AvgFrameTimer avg;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawGrade() => Instance.Grade.Draw();

    public void Draw()
    {
        avg.BeginSample();
        AppDraw.Section("Grade"u8, &DrawGrade);
        //DrawSection("Grade"u8, () => Grade.Draw());
        if (avg.EndSample() > 40) avg.ResetAndPrint("Fx");

       // DrawSection("White Balance"u8, static () => Instance.DrawWhiteBalance());
       // DrawSection("Bloom"u8, static () => Instance.DrawBloom());
       // DrawSection("ImageFx"u8, static () => Instance.DrawImageFx());
    }
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