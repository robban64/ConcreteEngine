using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.UI.MenuEntry;

namespace ConcreteEngine.Editor.UI;

internal sealed class MenuEntry(string name, SubItem[] subMenus)
{
    public readonly string Name = name;
    public readonly SubItem[] SubMenus = subMenus;
    public bool Enabled = true;
    public bool Visible = true;
    
    public sealed class SubItem(string name, string? shortcut, Action<StateManager> onClick)
    {
        public readonly string Name = name;
        public readonly String8Utf8 Shortcut = string.IsNullOrEmpty(shortcut) ? default(String8Utf8) : shortcut;
        public readonly Action<StateManager> OnClick = onClick;
        public bool Enabled = true;
        public bool Visible = true;
    }
}

internal sealed class MenuBar(StateManager state)
{
    public readonly List<MenuEntry> Entries = [
        new ("File", [
            new SubItem("Test",null, static (state) => {})
        ]),
        new ("Edit", [
            new SubItem("Test",null, static (state) => {})
        ]),
        new ("Debug", [
            new SubItem("Metrics", null, static (state) => state.ToggleDebugWindow(WindowManager.DebugMetricsWindow) )
        ]),
    ];

    public unsafe void Draw()
    {
        if (!ImGui.BeginMainMenuBar()) return;

        var sw = TextBuffers.GetWriter();
        foreach (var it in Entries)
        {
            if(!ImGui.BeginMenu(sw.Write(it.Name),it.Enabled)) continue;
            foreach (var subItem in it.SubMenus)
            {
                var shortcut = subItem.Shortcut;
                if (ImGui.MenuItem(sw.Write(it.Name), (byte*)&shortcut, it.Enabled))
                    subItem.OnClick(state);

            }
            ImGui.EndMenu();
        }

        ImGui.EndMainMenuBar();
    }
}