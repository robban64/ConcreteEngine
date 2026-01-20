using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Panels.Scene;
using ConcreteEngine.Editor.Panels.State;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class ScenePropertyPanel() : EditorPanel(PanelId.SceneProperty)
{
    private readonly SceneState _state = new();

    public override void Update()
    {
        if (Context.SceneProxy is not { } proxy) return;
        foreach (var property in proxy.Properties) property.Refresh();
        _state.Fill(CollectionsMarshal.AsSpan(proxy.Properties));
        _state.PreviousId = Context.SelectedSceneId;
    }

    public override void Draw(ref FrameContext ctx)
    {
        var proxy = Context.SceneProxy;
        if (proxy == null) return;

        if (!ImGui.BeginChild("##scene-props"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
            return;

        TextLayout.Make()
            .TitleSeparator(SpanWriterUtil.WriteTitleId(ref ctx.Sw, "Scene Object"u8, proxy.Id), Vector2.Zero)
            .Property("Name:"u8, ctx.Sw.Write(proxy.Name))
            .Property("GID:"u8, ctx.Sw.Write(proxy.GIdString))
            .RowSpace();


        foreach (var property in proxy.Properties)
        {
            switch (property)
            {
                case ProxyPropertyEntry<SpatialProperty> spatial:
                    DrawSceneProperty.DrawTransform(_state, spatial);
                    break;
                case ProxyPropertyEntry<SourceProperty> renderProp:
                    DrawSceneProperty.DrawRenderProperty(renderProp, ref ctx);
                    break;
                case ProxyPropertyEntry<ParticleProperty> particle:
                    DrawSceneProperty.DrawParticleProperty(_state, ref ctx);
                    break;
                case ProxyPropertyEntry<AnimationProperty>:
                    DrawSceneProperty.DrawAnimationProperty(_state, ref ctx);
                    break;
            }
        }

        ImGui.EndChild();
    }
}