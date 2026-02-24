using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Panels.Scene;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

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

        ImGui.SeparatorText(ref WriteFormat.WriteTitleId(ctx.Sw, "Scene Object"u8, proxy.Id));
        ImGui.Spacing();
        AppDraw.DrawTextProperty("Mesh:"u8, ref ctx.Sw.Write(props.SourceProperty.Mesh));
        ImGui.Spacing();
        AppDraw.DrawTextProperty("Material:"u8, ref ctx.Sw.Write(props.SourceProperty.MaterialId));

        DrawSceneProperty.DrawTransform(props.SpatialProperty);

        if (props.ParticleProperty is { } particle)
            DrawSceneProperty.DrawParticleProperty(particle, ctx.Sw);

        if (props.AnimationProperty is { } animation)
            DrawSceneProperty.DrawAnimationProperty(animation,  ctx);


    }
}