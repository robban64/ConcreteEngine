using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed class AtmospherePanel(StateManager state) : EditorPanel(PanelId.Atmosphere, state)
{
    public override void OnDraw()
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