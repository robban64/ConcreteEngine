using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.Lib;

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
internal sealed partial class PostEffectSettingsInspector : Inspector<PostEffectSettingsInspector>
{
    private static PostEffectSettings Target => VisualManager.Instance.PostEffect;

    public void Draw()
    {
        DrawSection("Grade"u8, static () => Instance.DrawGrade());
        DrawSection("White Balance"u8, static () => Instance.DrawWhiteBalance());
        DrawSection("Bloom"u8, static () => Instance.DrawBloom());
        DrawSection("ImageFx"u8, static () => Instance.DrawImageFx());
    }
}