using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.UI;

internal enum ToolbarGroupAlignment : byte { Left, Center, Right }

internal sealed class ToolbarGroup(ToolbarGroupAlignment alignment, ToolbarItem[] items)
{
    public readonly ToolbarItem[] Items = items;
    public readonly ToolbarGroupAlignment Alignment = alignment;
    public int VisibleCount = items.Length;
    public int Start;

    public float TotalWidth => VisibleCount * GuiTheme.TopbarHeight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Draw(StateManager ctx)
    {
        var start = int.Max(Start, 0);
        for (var i = start; i < Items.Length; i++)
        {
            if (i > start) ImGui.SameLine();
            Items[i].Draw(ctx);
        }
    }

    public void UpdateVisibleCount()
    {
        Start = -1;
        VisibleCount = 0;
        for (var i = 0; i < Items.Length; i++)
        {
            if (!Items[i].Visible) continue;

            if (Start == -1) Start = i;
            VisibleCount++;
        }
    }
}

internal sealed unsafe class ToolbarItem(
    Icons icon,
    ContextChangeMask changeMask,
    Action<StateManager> onClick,
    Action<EditorContext, EditorContext, ToolbarItem> onStateChange)
{
    private static readonly Vector2 BtnSize = new(GuiTheme.TopbarHeight);

    public readonly Action<StateManager> OnClick = onClick;
    public readonly Action<EditorContext, EditorContext, ToolbarItem> OnStateChange = onStateChange;

    public readonly uint Icon = StyleMap.GetIntIcon(icon);
    public readonly ContextChangeMask ChangeMask = changeMask;
    public bool Active;
    public bool Visible = true;

    private ImGuiSelectableFlags _flag = ImGuiSelectableFlags.None;

    public void Set(bool active, bool visible = true, bool enabled = true)
    {
        Active = active;
        Visible = visible;
        _flag = enabled ? 0 : ImGuiSelectableFlags.Disabled;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Draw(StateManager ctx)
    {
        if (!Visible) return;

        var icon = Icon;
        var clicked = ImGui.Selectable((byte*)&icon, Active, _flag, BtnSize);
        if (clicked) OnClick(ctx);
    }
}