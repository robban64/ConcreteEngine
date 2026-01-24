using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Panels.Scene;
using ConcreteEngine.Editor.UI;
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
        if (!ImGui.BeginChild("##scene"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
            return;

        var proxy = Context.SceneProxy;
        var props = proxy.Properties;

        var sw = ctx.Writer;

        TextLayout.Make()
            .TitleSeparator(SpanWriterUtil.WriteTitleId(ref sw, "Scene Object"u8, proxy.Id), false)
            .Property("Name:"u8, sw.Write(proxy.Name))
            .RowSpace().Property("Mesh:"u8, sw.Write(props.SourceProperty.Mesh.Value))
            .RowSpace().Property("Material:"u8, sw.Write(props.SourceProperty.MaterialId.Id));


        DrawSceneProperty.DrawTransform(props.SpatialProperty);

        if (props.ParticleProperty is { } particle)
            DrawSceneProperty.DrawParticleProperty(particle, sw);

        if (props.AnimationProperty is { } animation)
            DrawSceneProperty.DrawAnimationProperty(animation, in ctx);


        ImGui.EndChild();
    }
}