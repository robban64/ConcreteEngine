using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed class AtmospherePanel(StateContext context) : EditorPanel(PanelId.Atmosphere, context)
{
    public override void OnDraw(FrameContext ctx)
    {
        ImGui.SeparatorText("Atmosphere"u8);

        if (!ImGui.BeginTabBar("##tabs"u8)) return;

        if (ImGui.BeginTabItem("Fog"u8))
        {
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
}