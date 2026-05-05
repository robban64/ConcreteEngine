using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.UI.Core;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal static class TopMenuWindow
{
    public const int ToolbarGroupCount = 3;
    public const int MenuCount = 3;

    private static bool _hasInitialized;

    private static readonly ToolbarGroup[] Toolbar = new ToolbarGroup[ToolbarGroupCount];
    private static readonly MenuGroup[] MenuBar = new MenuGroup[MenuCount];

    public static ReadOnlySpan<ToolbarItem> GetToolbarGroup(ToolbarGroupAlignment i) => Toolbar[(int)i].Items;

    public static void SyncToolbar()
    {
        foreach (var it in Toolbar)
            it.UpdateVisibleCount();
    }

    public static void RegisterMenuToolbar()
    {
        if (_hasInitialized) throw new InvalidOperationException("Already registered");
        _hasInitialized = true;

        MenuBar[0] = FileMenu;
        MenuBar[1] = EditMenu;
        MenuBar[2] = DebugMenu;

        Toolbar[0] = new ToolbarGroup(ToolbarGroupAlignment.Left, [Asset, Scene]);
        Toolbar[1] = new ToolbarGroup(ToolbarGroupAlignment.Center, [Translate, Scale, Rotate, DebugBounds]);
        Toolbar[2] = new ToolbarGroup(ToolbarGroupAlignment.Right, [Selected, Camera, Lighting, Visual]);
    }

    public static void DrawMenu(StateManager stateManager)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, GuiTheme.MenuFramePadding);
        if (!ImGui.BeginMainMenuBar())
        {
            ImGui.PopStyleVar();
            return;
        }

        var sw = TextBuffers.GetWriter();
        foreach (var it in MenuBar)
            it.Draw(stateManager, sw);

        ImGui.EndMainMenuBar();
        ImGui.PopStyleVar();
    }


    public static void DrawToolbar(StateManager stateManager)
    {
        var width = ImGuiSystem.OutputSize.Width;
        ImGui.SetNextWindowPos(new Vector2(0, GuiTheme.MenuBarHeight));
        ImGui.SetNextWindowSize(new Vector2(width, GuiTheme.TopbarHeight));

        GuiTheme.PushFontIconLarge();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
        ImGui.PushStyleColor(ImGuiCol.Text, Palette32.White);
        ImGui.PushStyleColor(ImGuiCol.Header, Palette32.PrimaryColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Palette32.HoverColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, Palette32.SelectedColor);

        ToolbarGroup left = Toolbar[0], center = Toolbar[1], right = Toolbar[2];

        var centerX = float.Max(width * 0.5f - center.TotalWidth * 0.5f, left.TotalWidth);
        var rightX = float.Max(width - right.TotalWidth, centerX + center.TotalWidth) - right.VisibleCount * 6f;

        if (ImGui.Begin("##Topbar"u8, GuiTheme.TopbarFlags))
        {
            left.Draw(stateManager, 0);
            center.Draw(stateManager, centerX);
            right.Draw(stateManager, rightX);
        }

        ImGui.End();
        ImGui.PopStyleColor(4);
        ImGui.PopStyleVar(2);

        ImGui.PopFont();
    }

    private static readonly MenuGroup FileMenu = new("File", [
        new MenuItem("Test1", null, static (state) => { })
    ]);

    private static readonly MenuGroup EditMenu = new("Edit", [
        new MenuItem("Test2", null, static (state) => { })
    ]);

    private static readonly MenuGroup DebugMenu = new("Debug", [
        new MenuItem("Metrics", null,
            static (state) => state.ToggleDebugWindow(WindowManager.DebugMetricsWindow)),
        new MenuItem("ImGui Demo", null,
            static (state) => state.ToggleDebugWindow(WindowManager.DebugImDemoWindow)),
        new MenuItem("ImGui Profiler", null,
            static (state) => state.ToggleDebugWindow(WindowManager.DebugImMetricsWindow)),
        new MenuItem("ImGui Style", null,
            static (state) => state.ToggleDebugWindow(WindowManager.DebugImStyleWindow))
    ]);

    private static readonly ToolbarItem Asset = new(Icons.Database, ContextChangeMask.Mode,
        state => state.EnqueueEvent(new ModeEvent(ModeId.Asset)),
        (prev, next, it) => it.Set(next.Mode == ModeId.Asset));

    private static readonly ToolbarItem Scene = new(Icons.LayoutGrid, ContextChangeMask.Mode,
        state => state.EnqueueEvent(new ModeEvent(ModeId.Scene)),
        (prev, next, it) => it.Set(next.Mode == ModeId.Scene));

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