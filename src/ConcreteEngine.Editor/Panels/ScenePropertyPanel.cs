using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Panels.Scene;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class ScenePropertyPanel(PanelContext context) : EditorPanel(PanelId.SceneProperty,context)
{
    public override void Update()
    {
        Context.SceneProxy?.Refresh();
    }

    public override void Draw( in FrameContext ctx)
    {
        if (Context.SceneProxy == null) return;
        if (!ImGui.BeginChild("##scene-props"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
            return;

        var proxy = Context.SceneProxy;
        var sceneObject = proxy.SceneObject;
        var props = proxy.Properties;

        var sw = ctx.Writer;
        
        TextLayout.Make()
            .TitleSeparator(SpanWriterUtil.WriteTitleId(ref sw, "Scene Object"u8, proxy.Id), new Vector2(0, 1))
            .Property("Name:"u8, sw.Write(sceneObject.Name))
            .RowSpace();

        DrawSceneProperty.DrawRenderProperty(props.SourceProperty, in ctx);
        DrawSceneProperty.DrawTransform(props.SpatialProperty);

        if (props.ParticleProperty is { } particle)
            DrawSceneProperty.DrawParticleProperty(particle, in ctx);

        if (props.AnimationProperty is { } animation)
            DrawSceneProperty.DrawAnimationProperty(animation, in ctx);


        ImGui.EndChild();
    }
}