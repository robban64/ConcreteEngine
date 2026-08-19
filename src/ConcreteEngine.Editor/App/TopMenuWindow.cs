using System.Numerics;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.App.UI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

// ReSharper disable UnusedParameter.Local

namespace ConcreteEngine.Editor.App;

internal sealed class TopMenuWindow
{
    private const ImGuiWindowFlags TopbarFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
        ImGuiWindowFlags.NoScrollbar;

    public const int ToolbarGroupCount = 3;

    public static TopMenuWindow Instance { get; private set; } = null!;

    public static void Create()
    {
        if (Instance != null) throw new InvalidOperationException("Already registered");
        Instance = new TopMenuWindow();
    }

    private readonly MenuGroup _groupLeft;
    private readonly MenuGroup _groupCenter;
    private readonly MenuGroup _groupRight;

    private readonly ToolbarGroup _toolbarLeft;
    private readonly ToolbarGroup _toolbarCenter;
    private readonly ToolbarGroup _toolbarRight;

    private TopMenuWindow()
    {
        _groupLeft = new MenuGroup(StringArena.AllocateString("File"), [
            new MenuItem("Test1", null, static (state) => { })
        ]);
        _groupCenter = new MenuGroup(StringArena.AllocateString("Edit"), [
            new MenuItem("Test2", null, static (state) => { })
        ]);
        _groupRight = new MenuGroup(StringArena.AllocateString("Debug"), [
            new MenuItem("Metrics", null,
                static (state) => state.ToggleDebugWindow(WindowManager.DebugMetricsWindow)),
            new MenuItem("ImGui Demo", null,
                static (state) => state.ToggleDebugWindow(WindowManager.DebugImDemoWindow)),
            new MenuItem("ImGui Profiler", null,
                static (state) => state.ToggleDebugWindow(WindowManager.DebugImMetricsWindow)),
            new MenuItem("ImGui Style", null,
                static (state) => state.ToggleDebugWindow(WindowManager.DebugImStyleWindow))
        ]);


        _toolbarLeft = new ToolbarGroup(ToolbarGroupAlignment.Left, []);
        _toolbarCenter = new ToolbarGroup(ToolbarGroupAlignment.Center, [Translate, Scale, Rotate, DebugBounds]);
        _toolbarRight =
            new ToolbarGroup(ToolbarGroupAlignment.Right, [Selected, Camera, Lighting, Environment, PostFx]);
    }

    public ReadOnlySpan<ToolbarItem> GetToolbarGroup(ToolbarGroupAlignment i) => i switch
    {
        ToolbarGroupAlignment.Left => _toolbarLeft.Items,
        ToolbarGroupAlignment.Center => _toolbarCenter.Items,
        ToolbarGroupAlignment.Right => _toolbarRight.Items,
        _ => throw new ArgumentOutOfRangeException(nameof(i), i, null)
    };


    public void SyncToolbar()
    {
        _toolbarLeft.UpdateVisibleCount();
        _toolbarCenter.UpdateVisibleCount();
        _toolbarRight.UpdateVisibleCount();
    }

    public void Draw(StateManager stateManager)
    {
        DrawMenu(stateManager);
        DrawToolbar(stateManager);
    }

    public void DrawMenu(StateManager stateManager)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, GuiTheme.MenuFramePadding);
        if (ImGui.BeginMainMenuBar())
        {
            _groupLeft.Draw(stateManager);
            _groupCenter.Draw(stateManager);
            _groupRight.Draw(stateManager);

            ImGui.EndMainMenuBar();
        }

        ImGui.PopStyleVar();
    }


    public void DrawToolbar(StateManager stateManager)
    {
        var vp = ImGuiSystem.MainViewportPtr;
        var width = vp.WorkSize.X;

        PushToolbarStyles();
        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(new Vector2(width, GuiTheme.TopbarHeight));
        if (ImGui.Begin(WindowRoot.ToolbarWindowId, TopbarFlags))
        {
            var offsetX = GuiTheme.WindowPadding.X;
            var centerX = float.Max(width * 0.5f - _toolbarCenter.TotalWidth * 0.5f, _toolbarLeft.TotalWidth);
            var rightX = float.Max(width - _toolbarRight.TotalWidth, centerX + _toolbarCenter.TotalWidth) -
                         _toolbarRight.VisibleCount * 6f;

            ImGui.SetCursorPos(new Vector2(offsetX, 0));
            _toolbarLeft.Draw(stateManager);
            ImGui.SetCursorPos(new Vector2(centerX + offsetX, 0));
            _toolbarCenter.Draw(stateManager);
            ImGui.SetCursorPos(new Vector2(rightX - offsetX, 0));
            _toolbarRight.Draw(stateManager);
        }

        ImGui.End();
        PopToolbarStyles();
    }

    private static void PushToolbarStyles()
    {
        AppLayout.PushFontIcon();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));

        ImGui.PushStyleColor(ImGuiCol.Text, Palette32.White);
        ImGui.PushStyleColor(ImGuiCol.Header, Palette32.PrimaryColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Palette32.HoverColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, Palette32.SelectedColor);
    }

    private static void PopToolbarStyles()
    {
        ImGui.PopStyleColor(4);
        ImGui.PopStyleVar(2);
        ImGui.PopFont();
    }

    private static readonly ToolbarItem Translate = new(IconNames.Move3d, ContextChangeMask.Tool,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(TransformGizmoOp.Translate)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == TransformGizmoOp.Translate, visible: next.Tool.Enabled);
        });

    private static readonly ToolbarItem Scale = new(IconNames.Scale3d, ContextChangeMask.ToolSelection,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(TransformGizmoOp.Scale)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == TransformGizmoOp.Scale, visible: next.Tool.Enabled);
        });

    private static readonly ToolbarItem Rotate = new(IconNames.Rotate3d, ContextChangeMask.ToolSelection,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(TransformGizmoOp.Rotate)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == TransformGizmoOp.Rotate, visible: next.Tool.Enabled);
        });

    private static readonly ToolbarItem DebugBounds = new(IconNames.Box, ContextChangeMask.ToolSelection,
        state => state.EnqueueEvent(ToolEvent.MakeBounds(!state.Context.Tool.ShowDebugBounds)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.ShowDebugBounds, visible: next.Selection.HasSceneObject);
        });

    private static readonly ToolbarItem Selected = new(IconNames.MousePointer2, ContextChangeMask.Selection,
        state => { },
        (prev, next, it) => it.Set(false, next.Selection.HasSceneObject));

    private static readonly ToolbarItem Camera = new(IconNames.Video, ContextChangeMask.Selection,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Camera)),
        (prev, next, it) => it.Set(next.Selection.HasNewFixed(prev.Selection, FixedInspectorId.Camera)));

    private static readonly ToolbarItem Lighting = new(IconNames.Sun, ContextChangeMask.Selection,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Lighting)),
        (prev, next, it) => it.Set(next.Selection.HasNewFixed(prev.Selection, FixedInspectorId.Lighting)));

    private static readonly ToolbarItem Environment = new(IconNames.CloudFog, ContextChangeMask.Selection,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Environment)),
        (prev, next, it) => it.Set(next.Selection.HasNewFixed(prev.Selection, FixedInspectorId.Environment)));

    private static readonly ToolbarItem PostFx = new(IconNames.Sparkles, ContextChangeMask.Selection,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.PostFx)),
        (prev, next, it) => it.Set(next.Selection.HasNewFixed(prev.Selection, FixedInspectorId.PostFx)));
}