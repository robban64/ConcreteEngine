using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Assets;
using ConcreteEngine.Editor.UI.Metrics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor.Core;

internal sealed class WindowManager(StateManager stateManager)
{
    private const ImGuiWindowFlags TopbarFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
        ImGuiWindowFlags.NoScrollbar;

    private const ImGuiWindowFlags ConsoleFlags =
        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;


    private const int WindowCount = 3;
    private const int ToolbarGroupCount = 3;

    private readonly Dictionary<Type, EditorPanel> _panelDict = new(16);

    private readonly EditorWindow[] _windows = new EditorWindow[WindowCount];
    private readonly ToolbarGroup[] _toolbar = new ToolbarGroup[ToolbarGroupCount];

    public EditorPanel GetPanel(Type type) => _panelDict[type];
    public T GetPanel<T>() where T : EditorPanel => (T)_panelDict[typeof(T)];
    public EditorWindow GetWindow(WindowId windowId) => _windows[(int)windowId];
    public ReadOnlySpan<ToolbarItem> GetToolbarGroup(ToolbarGroupAlignment i) => _toolbar[(int)i].Items;

    public void UpdateDiagnostic()
    {
        foreach (var window in _windows)
            window.OnUpdateDiagnostic();
    }

    public void SyncToolbar()
    {
        foreach (var it in _toolbar)
            it.UpdateVisibleCount();
    }

    public void Draw()
    {
        GuiTheme.PushFontIconLarge();
        DrawToolbar();
        ImGui.PopFont();

        GuiTheme.PushFontText();
        foreach (var window in _windows)
        {
            window.OnDraw();
        }
    }

    private void DrawToolbar()
    {
        var size = new Vector2(ImGuiSystem.OutputSize.Width, GuiTheme.TopbarHeight);
        ImGui.SetNextWindowSize(size);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        if (ImGui.Begin("topbar"u8, TopbarFlags))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
            ImGui.PushStyleColor(ImGuiCol.Text, Palette32.White);
            ImGui.PushStyleColor(ImGuiCol.Header, Palette32.PrimaryColor);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Palette32.HoverColor);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, Palette32.SelectedColor);

            DrawItems();

            ImGui.PopStyleColor(4);
            ImGui.PopStyleVar();
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private void DrawItems()
    {
        var width = ImGuiSystem.OutputSize.Width;

        ToolbarGroup left = _toolbar[0], center = _toolbar[1], right = _toolbar[2];

        var centerX = float.Max(width * 0.5f - center.TotalWidth * 0.5f, left.TotalWidth);
        var rightX = float.Max(width - right.TotalWidth, centerX + center.TotalWidth) - right.VisibleCount * 6f;

        left.Draw(stateManager);
        ImGui.SetCursorPos(new Vector2(centerX, 0));
        center.Draw(stateManager);
        ImGui.SetCursorPos(new Vector2(rightX, 0));
        right.Draw(stateManager);
    }

    public void Navigate(WindowId windowId, Type panelType)
    {
        GetWindow(windowId).EnqueuePanel(GetPanel(panelType));
    }

    public void Init(StateManager ctx, ConsoleService consoleService)
    {
        RegisterPanels(ctx, consoleService);
        RegisterToolbar();
        var leftWindow = _windows[(int)WindowId.Left] = new EditorWindow("LeftSidebar", WindowId.Left, ctx);
        var rightWindow = _windows[(int)WindowId.Right] = new EditorWindow("RightSidebar", WindowId.Right, ctx);
        var bottomWindow = _windows[(int)WindowId.Bottom] = new EditorWindow("Console", WindowId.Bottom, ctx);

        rightWindow.Layout.WindowPadding = GuiTheme.WindowPaddingX2;
        bottomWindow.Layout.WindowPadding = GuiTheme.WindowPaddingX2;
        bottomWindow.Layout.BgColor = ConsolePanel.ConsoleBgColor;
        bottomWindow.Flags = ConsoleFlags;

        foreach (var it in _panelDict.Values)
        {
            it.OnCreate();
        }

        Navigate(WindowId.Left, typeof(AssetListPanel));
        Navigate(WindowId.Right, typeof(CameraPanel));
        Navigate(WindowId.Bottom, typeof(ConsolePanel));

        SyncToolbar();
    }

    private void RegisterToolbar()
    {
        _toolbar[0] = new ToolbarGroup(ToolbarGroupAlignment.Left, [Metric, Main, Play]);
        _toolbar[1] = new ToolbarGroup(ToolbarGroupAlignment.Center, [Translate, Scale, Rotate, DebugBounds]);
        _toolbar[2] = new ToolbarGroup(ToolbarGroupAlignment.Right, [Selected, Camera, Lighting, Atmosphere, Visual]);
    }

    private void RegisterPanels(StateManager ctx, ConsoleService consoleService)
    {
        RegisterPanel(new MetricsLeftPanel(ctx));
        RegisterPanel(new MetricsRightPanel(ctx));

        RegisterPanel(new AssetListPanel(ctx));
        RegisterPanel(new AssetInspectorPanel(ctx));

        RegisterPanel(new SceneListPanel(ctx));
        RegisterPanel(new SceneInspectorPanel(ctx));

        RegisterPanel(new CameraPanel(ctx));
        RegisterPanel(new AtmospherePanel(ctx));
        RegisterPanel(new LightingPanel(ctx));
        RegisterPanel(new VisualPanel(ctx));

        RegisterPanel(new ConsolePanel(ctx, consoleService));
    }


    private void RegisterPanel<T>(T panel) where T : EditorPanel
    {
        ArgumentNullException.ThrowIfNull(panel);
        _panelDict.Add(typeof(T), panel);
    }

    private static readonly ToolbarItem Metric = new(Icons.Activity,
        state => state.EnqueueEvent(new ModeEvent { MetricMode = true }),
        (prev, next, it) => it.Set(next.Mode.IsMetricMode));

    private static readonly ToolbarItem Main = new(Icons.LayoutGrid,
        state => state.EnqueueEvent(new ModeEvent { MetricMode = false }),
        (prev, next, it) => it.Set(!next.Mode.IsMetricMode));


    private static readonly ToolbarItem Play = new(Icons.Play,
        state => state.EnqueueEvent(new ModeEvent { MetricMode = false }),
        (prev, next, it) => it.Set(false));

    private static readonly ToolbarItem Translate = new(Icons.Move3d,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(ImGuizmoOperation.Translate)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == ImGuizmoOperation.Translate, visible: next.Tool.GizmoEnabled);
        });

    private static readonly ToolbarItem Scale = new(Icons.Scale3d,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(ImGuizmoOperation.Scale)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == ImGuizmoOperation.Scale, visible: next.Tool.GizmoEnabled);
        });

    private static readonly ToolbarItem Rotate = new(Icons.Rotate3d,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(ImGuizmoOperation.Rotate)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == ImGuizmoOperation.Rotate, visible: next.Tool.GizmoEnabled);
        });

    private static readonly ToolbarItem DebugBounds = new(Icons.Box,
        state => state.EnqueueEvent(ToolEvent.MakeBounds(!state.Context.Tool.ShowDebugBounds)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == ImGuizmoOperation.Translate, visible: next.Tool.GizmoEnabled);
        });


    private static readonly ToolbarItem Selected = new(Icons.MousePointer2, ctx => { },
        (prev, next, it) => it.Set(false, next.Selection.HasSceneObject));

    private static readonly ToolbarItem Camera = new(Icons.Video,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Camera)),
        (prev, next, it) => it.Set(next.Selection.IsNew(prev.Selection, FixedInspectorId.Camera)));

    private static readonly ToolbarItem Lighting = new(Icons.Sun,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Lighting)),
        (prev, next, it) => it.Set(next.Selection.IsNew(prev.Selection, FixedInspectorId.Lighting)));


    private static readonly ToolbarItem Atmosphere = new(Icons.CloudFog,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Atmosphere)),
        (prev, next, it) => it.Set(next.Selection.IsNew(prev.Selection, FixedInspectorId.Atmosphere)));


    private static readonly ToolbarItem Visual = new(Icons.Sparkles,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Visual)),
        (prev, next, it) => it.Set(next.Selection.IsNew(prev.Selection, FixedInspectorId.Visual)));
}