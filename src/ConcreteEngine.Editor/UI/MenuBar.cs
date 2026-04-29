using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

public sealed class MenuBar()
{
    public void Draw()
    {
        if (!ImGui.BeginMenuBar()) return;

        if (ImGui.BeginMenu("File"u8))
        {
            //if (ImGui.MenuItem("Save", "Ctrl+S")) {}
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Edit"u8))
        {
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Scene"u8))
        {
            ImGui.EndMenu();
        }
        
        if (ImGui.BeginMenu("Debug"u8))
        {
            ImGui.EndMenu();
        }

        ImGui.EndMenuBar();

    }
}