using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Inspector;

internal sealed class ShaderInspectorUi(PanelContext panelContext, AssetController assetController)
{
    public void Draw(InspectShader editShader,  in FrameContext ctx)
    {
        ImGui.Spacing();

        if (ImGui.Button("Reload Shader"u8, new Vector2(-1, 0)))
            panelContext.EnqueueEvent(new AssetReloadEvent(editShader.Asset.Name));

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Recompiles source files."u8);

    }
}