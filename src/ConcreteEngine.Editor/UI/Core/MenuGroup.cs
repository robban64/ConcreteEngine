using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Core;

internal sealed class MenuGroup(string name, MenuItem[] items)
{
    public readonly string Name = name;
    public readonly MenuItem[] Items = items;
    public bool Enabled = true;
    public bool Visible = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Draw(StateManager stateManager, UnsafeSpanWriter sw)
    {
        if (!Visible || !ImGui.BeginMenu(sw.Write(Name), Enabled)) return;
        foreach (var it in Items)
        {
            if (!it.Visible) continue;

            var shortcut = it.Shortcut;
            if (ImGui.MenuItem(sw.Write(it.Name), (byte*)&shortcut, it.Enabled))
                it.OnClick(stateManager);
        }

        ImGui.EndMenu();
    }
}

internal sealed class MenuItem(string name, string? shortcut, Action<StateManager> onClick)
{
    public readonly string Name = name;
    public readonly String8Utf8 Shortcut = string.IsNullOrEmpty(shortcut) ? default(String8Utf8) : shortcut;
    public readonly Action<StateManager> OnClick = onClick;
    public bool Enabled = true;
    public bool Visible = true;
}