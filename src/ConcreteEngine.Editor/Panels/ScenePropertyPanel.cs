using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Panels.Scene;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class ScenePropertyPanel() : EditorPanel(PanelId.SceneProperty)
{
    public override void Update()
    {
        Context.SceneProxy?.Refresh();
    }

    public override void Draw(ref FrameContext ctx)
    {
        if (Context.SceneProxy == null) return;
        if (!ImGui.BeginChild("##scene-props"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
            return;

        var proxy = Context.SceneProxy;
        var sceneObject = proxy.SceneObject;
        var props = proxy.Properties;

        TextLayout.Make()
            .TitleSeparator(SpanWriterUtil.WriteTitleId(ref ctx.Sw, "Scene Object"u8, proxy.Id), new Vector2(0, 1))
            .Property("Name:"u8, ctx.Sw.Write(sceneObject.Name))
            .RowSpace();

        DrawSceneProperty.DrawRenderProperty(props.SourceProperty, ref ctx);
        DrawSceneProperty.DrawTransform(props.SpatialProperty);

        if (props.ParticleProperty is { } particle)
            DrawSceneProperty.DrawParticleProperty(particle, ref ctx);

        if (props.AnimationProperty is { } animation)
            DrawSceneProperty.DrawAnimationProperty(animation, ref ctx);


        ImGui.EndChild();
    }
}