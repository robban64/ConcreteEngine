using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.App.UI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App;

internal sealed class TopMenuWindow
{
    private const ImGuiWindowFlags TopbarFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
        ImGuiWindowFlags.NoScrollbar;

    public const int ToolbarGroupCount = 3;
    public const int MenuCount = 3;

    public static readonly TopMenuWindow Instance = new();

    private bool _hasInitialized;

    private MemoryBlockPtr _memory;

    private readonly MenuGroup[] _menuBar = new MenuGroup[MenuCount];
    private readonly ToolbarGroup[] _toolbar = new ToolbarGroup[ToolbarGroupCount];

    public ReadOnlySpan<ToolbarItem> GetToolbarGroup(ToolbarGroupAlignment i) => _toolbar[(int)i].Items;

    public void RegisterMenuToolbar()
    {
        if (_hasInitialized) throw new InvalidOperationException("Already registered");
        _hasInitialized = true;

        _menuBar[0] = new MenuGroup(StringArena.AllocateString("File"), [
            new MenuItem("Test1", null, static (state) => { })
        ]);
        _menuBar[1] = new MenuGroup(StringArena.AllocateString("Edit"), [
            new MenuItem("Test2", null, static (state) => { })
        ]);
        _menuBar[2] = new MenuGroup(StringArena.AllocateString("Debug"), [
            new MenuItem("Metrics", null,
                static (state) => state.ToggleDebugWindow(WindowManager.DebugMetricsWindow)),
            new MenuItem("ImGui Demo", null,
                static (state) => state.ToggleDebugWindow(WindowManager.DebugImDemoWindow)),
            new MenuItem("ImGui Profiler", null,
                static (state) => state.ToggleDebugWindow(WindowManager.DebugImMetricsWindow)),
            new MenuItem("ImGui Style", null,
                static (state) => state.ToggleDebugWindow(WindowManager.DebugImStyleWindow))
        ]);


        _toolbar[0] = new ToolbarGroup(ToolbarGroupAlignment.Left, []);
        _toolbar[1] = new ToolbarGroup(ToolbarGroupAlignment.Center, [Translate, Scale, Rotate, DebugBounds]);
        _toolbar[2] = new ToolbarGroup(ToolbarGroupAlignment.Right, [Selected, Camera, Lighting, Visual]);
    }
    
    
    public void SyncToolbar()
    {
        foreach (var it in _toolbar) it.UpdateVisibleCount();
    }

    public void Draw(StateManager stateManager)
    {
        DrawMenu(stateManager);
        DrawToolbar(stateManager);
    }
    
    public void DrawMenu(StateManager stateManager)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, GuiTheme.MenuFramePadding);
        if (!ImGui.BeginMainMenuBar())
        {
            ImGui.PopStyleVar();
            return;
        }

        foreach (var it in _menuBar)
            it.Draw(stateManager);

        ImGui.EndMainMenuBar();
        ImGui.PopStyleVar();
    }


    public void DrawToolbar(StateManager stateManager)
    {
        var vp = ImGuiSystem.MainViewportPtr;
        var width = vp.WorkSize.X;

        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(new Vector2(width, GuiTheme.TopbarHeight));

        GuiTheme.PushFontIcon();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));

        ImGui.PushStyleColor(ImGuiCol.Text, Palette32.White);
        ImGui.PushStyleColor(ImGuiCol.Header, Palette32.PrimaryColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Palette32.HoverColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, Palette32.SelectedColor);

        ToolbarGroup left = _toolbar[0], center = _toolbar[1], right = _toolbar[2];

        var centerX = float.Max(width * 0.5f - center.TotalWidth * 0.5f, left.TotalWidth);
        var rightX = float.Max(width - right.TotalWidth, centerX + center.TotalWidth) - right.VisibleCount * 6f;

        if (ImGui.Begin(WindowRoot.ToolbarWindowId, TopbarFlags))
        {
            var offsetX = GuiTheme.WindowPadding.X;
            ImGui.SetCursorPos(new Vector2(offsetX, 0));
            left.Draw(stateManager);
            ImGui.SetCursorPos(new Vector2(centerX + offsetX, 0));
            center.Draw(stateManager);
            ImGui.SetCursorPos(new Vector2(rightX - offsetX, 0));
            right.Draw(stateManager);
        }

        ImGui.End();
        ImGui.PopStyleColor(4);
        ImGui.PopStyleVar(2);

        ImGui.PopFont();
    }

    private static readonly ToolbarItem Translate = new(Icons.Move3d, ContextChangeMask.Tool,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(TransformGizmoOp.Translate)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == TransformGizmoOp.Translate, visible: next.Tool.Enabled);
        });

    private static readonly ToolbarItem Scale = new(Icons.Scale3d, ContextChangeMask.ToolSelection,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(TransformGizmoOp.Scale)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == TransformGizmoOp.Scale, visible: next.Tool.Enabled);
        });

    private static readonly ToolbarItem Rotate = new(Icons.Rotate3d, ContextChangeMask.ToolSelection,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(TransformGizmoOp.Rotate)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == TransformGizmoOp.Rotate, visible: next.Tool.Enabled);
        });

    private static readonly ToolbarItem DebugBounds = new(Icons.Box, ContextChangeMask.ToolSelection,
        state => state.EnqueueEvent(ToolEvent.MakeBounds(!state.Context.Tool.ShowDebugBounds)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.ShowDebugBounds, visible: next.Selection.HasSceneObject);
        });

    private static readonly ToolbarItem Selected = new(Icons.MousePointer2, ContextChangeMask.Selection,
        state => { },
        (prev, next, it) => it.Set(false, next.Selection.HasSceneObject));

    private static readonly ToolbarItem Camera = new(Icons.Video, ContextChangeMask.Selection,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Camera)),
        (prev, next, it) => it.Set(next.Selection.HasNew(prev.Selection, FixedInspectorId.Camera)));

    private static readonly ToolbarItem Lighting = new(Icons.Sun, ContextChangeMask.Selection,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Lighting)),
        (prev, next, it) => it.Set(next.Selection.HasNew(prev.Selection, FixedInspectorId.Lighting)));

    private static readonly ToolbarItem Visual = new(Icons.Sparkles, ContextChangeMask.Selection,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Visual)),
        (prev, next, it) => it.Set(next.Selection.HasNew(prev.Selection, FixedInspectorId.Visual)));
}