using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Inspector;

internal sealed class ShaderInspectorUi(PanelContext panelContext, AssetController assetController)
{
    public void Draw(InspectShader editShader, FrameContext ctx)
    {
        ImGui.Spacing();

        if (ImGui.Button("Reload Shader"u8, new Vector2(-1, 0)))
            panelContext.EnqueueEvent(new AssetUpdateEvent(AssetUpdateEvent.EventAction.Reload, editShader.Id));

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Recompiles source files."u8);
    }
}