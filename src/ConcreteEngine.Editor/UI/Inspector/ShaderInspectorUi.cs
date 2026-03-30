using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Inspector;

internal sealed class ShaderInspectorUi(StateContext panelContext)
{
    public void Draw(InspectShader editShader, FrameContext ctx)
    {
        ImGui.Spacing();
        var width = GuiLayout.GetRowWidthForItems(2);
        if (ImGui.Button("Open"u8, new Vector2(width, 0)))
            panelContext.EnqueueEvent(new AssetEvent(EditorEvent.EventAction.Reload, editShader.Id));

        ImGui.SameLine();

        if (ImGui.Button("Reload"u8, new Vector2(width, 0)))
            panelContext.EnqueueEvent(new AssetEvent(EditorEvent.EventAction.Reload, editShader.Id));
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Recompiles source files."u8);
    }
}