using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(PostEffectSettings))]
internal sealed partial class PostEffectSettingsInspector : Inspector<PostEffectSettings>
{
    public override uint Icon => IconNames.Sparkles;
    public override InspectorId Id => InspectorId.PostFx;

    public PostEffectSettingsInspector()
    {
        Sections = _fields.CreateSections();
        AttachTarget(VisualManager.Instance.PostEffect);
    }

    public override void Draw()
    {
        foreach (var section in Sections) section.Draw();
    }
}