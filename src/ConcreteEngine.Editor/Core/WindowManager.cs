using System.Numerics;
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
using static ConcreteEngine.Editor.Core.WindowManagerStore;
    
namespace ConcreteEngine.Editor.Core;

internal sealed class WindowManager(StateManager stateManager)
{
    private const int WindowCount = 3;
    private const int ToolbarGroupCount = 3; 
    
    public const int DebugMetricsWindow = 0;
    public const int DebugImDemoWindow = 1;
    public const int DebugImMetricsWindow = 2;
    public const int DebugImStyleWindow = 3;

    public  int ActiveDebugWindow { get;  private set; } = -1;

    private readonly EditorWindow[] _windows = new EditorWindow[WindowCount];
    private readonly ToolbarGroup[] _toolbar = new ToolbarGroup[ToolbarGroupCount];
    private readonly MenuBar _menuBar = new(stateManager);
    
    private readonly Dictionary<Type, EditorPanel> _panelDict = new(16);

    private readonly Action[] _debugWindows = new Action[4];
    
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

    public void Navigate(WindowId windowId, Type panelType)
    {
        GetWindow(windowId).EnqueuePanel(GetPanel(panelType));
    }
    

    public void Draw()
    {
        _menuBar.Draw();
        
        GuiTheme.PushFontIconLarge();
        DrawToolbar();
        ImGui.PopFont();

        //GuiTheme.PushFontText();
        foreach (var window in _windows)
        {
            window.OnDraw();
        }

        if (ActiveDebugWindow >= 0)
            _debugWindows[ActiveDebugWindow]();
    }

    public void ToggleDebugWindow(int id)
    {
        if (id < 0)
        {
            ActiveDebugWindow = -1;
            return;
        }
        
        if(id >= _windows.Length)
            throw new ArgumentOutOfRangeException($"Invalid debug window id {id}",nameof(id));
        
        ActiveDebugWindow = id;
    }



    public void Init(StateManager ctx, ConsoleService consoleService)
    {
        RegisterWindows(ctx);
        RegisterPanels(ctx, consoleService);
        RegisterToolbar();

        RegisterDebugWindows();
        
        foreach (var it in _panelDict.Values)
            it.OnCreate();

        Navigate(WindowId.Left, typeof(AssetListPanel));
        Navigate(WindowId.Right, typeof(CameraPanel));
        Navigate(WindowId.Bottom, typeof(ConsolePanel));

        SyncToolbar();
    }
    private void DrawToolbar()
    {
        var size = new Vector2(ImGuiSystem.OutputSize.Width, GuiTheme.TopbarHeight);
        ImGui.SetNextWindowSize(size);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        if (ImGui.Begin("topbar"u8, GuiTheme.TopbarFlags))
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

    private void RegisterDebugWindows()
    {
        _debugWindows[DebugMetricsWindow] = MetricsUi.Draw;
        _debugWindows[DebugImDemoWindow] = ImGui.ShowDemoWindow;
        _debugWindows[DebugImMetricsWindow] = ImGui.ShowMetricsWindow;
        _debugWindows[DebugImStyleWindow] = ImGui.ShowStyleEditor;

        stateManager.DebugWindowChanged = ToggleDebugWindow;
    }
    
    private void RegisterWindows(StateManager ctx)
    {
        var leftWindow = _windows[(int)WindowId.Left] = new EditorWindow("LeftSidebar", WindowId.Left, ctx);
        var rightWindow = _windows[(int)WindowId.Right] = new EditorWindow("RightSidebar", WindowId.Right, ctx);
        var bottomWindow = _windows[(int)WindowId.Bottom] = new EditorWindow("Console", WindowId.Bottom, ctx);

        leftWindow.Layout.WindowPadding = GuiTheme.WindowPaddingSlim;
        bottomWindow.Layout.BgColor = ConsolePanel.ConsoleBgColor;
        bottomWindow.Flags = GuiTheme.ConsoleFlags;
    }

    private void RegisterToolbar()
    {
        _toolbar[0] = new ToolbarGroup(ToolbarGroupAlignment.Left, [Metric, Asset, Scene]);
        _toolbar[1] = new ToolbarGroup(ToolbarGroupAlignment.Center, [Translate, Scale, Rotate, DebugBounds]);
        _toolbar[2] = new ToolbarGroup(ToolbarGroupAlignment.Right, [Selected, Camera, Lighting, Visual]);
        
        
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
        RegisterPanel(new LightingPanel(ctx));
        RegisterPanel(new VisualPanel(ctx));

        RegisterPanel(new ConsolePanel(ctx, consoleService));
    }

    private void RegisterPanel<T>(T panel) where T : EditorPanel
    {
        ArgumentNullException.ThrowIfNull(panel);
        _panelDict.Add(typeof(T), panel);
    }
}

file static  class WindowManagerStore{

    public static readonly ToolbarItem Metric = new(Icons.Activity, ContextChangeMask.Mode,
        state => state.EnqueueEvent(new ModeEvent { Mode = ModeId.Metric }),
        (prev, next, it) => it.Set(next.Mode == ModeId.Metric));

    public static readonly ToolbarItem Asset = new(Icons.File, ContextChangeMask.Mode,
        state => state.EnqueueEvent(new ModeEvent { Mode = ModeId.Asset }),
        (prev, next, it) => it.Set(next.Mode == ModeId.Asset));
    public static readonly ToolbarItem Scene = new(Icons.LayoutGrid, ContextChangeMask.Mode,
        state => state.EnqueueEvent(new ModeEvent { Mode = ModeId.Scene }),
        (prev, next, it) => it.Set(next.Mode == ModeId.Scene));

    public static readonly ToolbarItem Translate = new(Icons.Move3d, ContextChangeMask.Tool,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(ImGuizmoOperation.Translate)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == ImGuizmoOperation.Translate, visible: next.Tool.GizmoEnabled);
        });

    public static readonly ToolbarItem Scale = new(Icons.Scale3d, ContextChangeMask.ToolSelection,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(ImGuizmoOperation.Scale)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == ImGuizmoOperation.Scale, visible: next.Tool.GizmoEnabled);
        });

    public static readonly ToolbarItem Rotate = new(Icons.Rotate3d, ContextChangeMask.ToolSelection,
        state => state.EnqueueEvent(ToolEvent.MakeGizmo(ImGuizmoOperation.Rotate)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoOp == ImGuizmoOperation.Rotate, visible: next.Tool.GizmoEnabled);
        });

    public static readonly ToolbarItem DebugBounds = new(Icons.Box, ContextChangeMask.ToolSelection,
        state => state.EnqueueEvent(ToolEvent.MakeBounds(!state.Context.Tool.ShowDebugBounds)),
        (prev, next, it) =>
        {
            it.Set(next.Tool.GizmoEnabled, visible: next.Selection.HasSceneObject);
        });

    public static readonly ToolbarItem Selected = new(Icons.MousePointer2, ContextChangeMask.Selection,
        state => { },
        (prev, next, it) => it.Set(false, next.Selection.HasSceneObject));

    public static readonly ToolbarItem Camera = new(Icons.Video, ContextChangeMask.Selection,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Camera)),
        (prev, next, it) => it.Set(next.Selection.IsNew(prev.Selection, FixedInspectorId.Camera)));

    public static readonly ToolbarItem Lighting = new(Icons.Sun, ContextChangeMask.Selection,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Lighting)),
        (prev, next, it) => it.Set(next.Selection.IsNew(prev.Selection, FixedInspectorId.Lighting)));

    public static readonly ToolbarItem Visual = new(Icons.Sparkles, ContextChangeMask.Selection,
        state => state.EnqueueEvent(new SelectionEvent(FixedInspectorId.Visual)),
        (prev, next, it) => it.Set(next.Selection.IsNew(prev.Selection, FixedInspectorId.Visual)));
}