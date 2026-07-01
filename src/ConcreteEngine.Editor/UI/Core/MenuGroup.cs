using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed class MenuGroup(NativeString name, MenuItem[] items)
{
    //public readonly string Name = name;
    public readonly NativeString Name = name;
    public readonly MenuItem[] Items = items;
    public bool Enabled = true;
    public bool Visible = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Draw(StateManager stateManager)
    {
        if (!Visible) return;
        if (ImGui.BeginMenu(Name, Enabled))
        {
            DrawChildren(stateManager);
            ImGui.EndMenu();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void DrawChildren(StateManager stateManager)
    {
        var sw = TextBuffers.GetWriter();
        foreach (var it in Items)
        {
            if (!it.Visible) continue;

            var shortcut = it.Shortcut;
            if (ImGui.MenuItem(sw.Write(it.Name), (byte*)&shortcut, it.Enabled))
                it.OnClick(stateManager);
        }

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