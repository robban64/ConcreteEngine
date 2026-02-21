using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Panels.Scene;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Panels;

internal sealed class ScenePropertyPanel(PanelContext context) : EditorPanel(PanelId.SceneProperty, context)
{
    public override void Update()
    {
        Context.SceneProxy?.Refresh();
    }

    public override void Draw(in FrameContext ctx)
    {
        if (Context.SceneProxy == null) return;

        var proxy = Context.SceneProxy;
        var props = proxy.Properties;

        var sw = ctx.Writer;

        TextLayout.Make()
            .TitleSeparator(ref WriteFormat.WriteTitleId(sw, "Scene Object"u8, proxy.Id), padUp: false)
            .Property("Name:"u8, ref sw.Write(proxy.Name))
            .RowSpace().Property("Mesh:"u8, ref sw.Write(props.SourceProperty.Mesh))
            .RowSpace().Property("Material:"u8, ref sw.Write(props.SourceProperty.MaterialId));


        DrawSceneProperty.DrawTransform(props.SpatialProperty);

        if (props.ParticleProperty is { } particle)
            DrawSceneProperty.DrawParticleProperty(particle, sw);

        if (props.AnimationProperty is { } animation)
            DrawSceneProperty.DrawAnimationProperty(animation, in ctx);


    }
}