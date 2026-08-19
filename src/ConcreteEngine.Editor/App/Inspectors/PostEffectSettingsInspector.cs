using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(PostEffectSettings))]
internal sealed partial class PostEffectSettingsInspector : Inspector<PostEffectSettingsInspector>
{
    private static PostEffectSettings Target => VisualManager.Instance.PostEffect;
    public override uint Icon => IconNames.Sparkles;

    public void Draw()
    {
        foreach (var section in Sections) section.Draw();
    }
}