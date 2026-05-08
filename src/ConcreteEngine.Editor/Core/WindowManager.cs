using System.Numerics;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Assets;
using Hexa.NET.ImGui;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedVariable

namespace ConcreteEngine.Editor.Core;

internal sealed class WindowManager(StateManager stateManager)
{
    private const int WindowCount = 3;
    public const int DebugWindowCount = 4;

    public const int DebugMetricsWindow = 0;
    public const int DebugImDemoWindow = 1;
    public const int DebugImMetricsWindow = 2;
    public const int DebugImStyleWindow = 3;
    
    //
    private readonly Dictionary<Type, EditorPanel> _panelDict = new(16);
    private readonly EditorWindow[] _windows = new EditorWindow[WindowCount];
    private readonly Action[] _debugWindows = new Action[DebugWindowCount];

    public EditorPanel GetPanel(Type type) => _panelDict[type];
    public T GetPanel<T>() where T : EditorPanel => (T)_panelDict[typeof(T)];
    public EditorWindow GetWindow(WindowId windowId) => _windows[(int)windowId];

    public void UpdateDiagnostic()
    {
        foreach (var window in _windows)
            window.OnUpdateDiagnostic();
    }

    public void Navigate(WindowId windowId, Type panelType)
    {
        GetWindow(windowId).EnqueuePanel(GetPanel(panelType));
    }


    public void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        WindowRoot.BeginDockSpace();
        TopMenuWindow.DrawMenu(stateManager);
        TopMenuWindow.DrawToolbar(stateManager);
        ViewportWindow.Draw(stateManager);
        ImGui.PopStyleVar();
        
        foreach (var window in _windows)
            window.OnDraw();

        if ((uint)stateManager.ActiveDebugWindow < (uint)_debugWindows.Length)
            _debugWindows[stateManager.ActiveDebugWindow]();
    }

    public void Init(ConsoleService consoleService)
    {
        RegisterWindows();
        RegisterPanels(consoleService);
        TopMenuWindow.RegisterMenuToolbar();

        RegisterDebugWindows();

        foreach (var it in _panelDict.Values)
            it.OnCreate();

        Navigate(WindowId.Left, typeof(AssetListPanel));
        Navigate(WindowId.Right, typeof(CameraPanel));
        Navigate(WindowId.Bottom, typeof(ConsolePanel));

        TopMenuWindow.SyncToolbar();
    }


    private void RegisterDebugWindows()
    {
        _debugWindows[DebugMetricsWindow] = MetricsUi.Draw;
        _debugWindows[DebugImDemoWindow] = ImGui.ShowDemoWindow;
        _debugWindows[DebugImMetricsWindow] = ImGui.ShowMetricsWindow;
        _debugWindows[DebugImStyleWindow] = ImGui.ShowStyleEditor;
    }

    private void RegisterWindows()
    {
        var leftWindow = _windows[(int)WindowId.Left] = new EditorWindow("##Left", WindowId.Left);
        var rightWindow = _windows[(int)WindowId.Right] = new EditorWindow("##Right", WindowId.Right);
        var bottomWindow = _windows[(int)WindowId.Bottom] = new EditorWindow("##Bottom", WindowId.Bottom);

        leftWindow.Memory = TextBuffers.WindowMemory1;
        rightWindow.Memory = TextBuffers.WindowMemory2;
        bottomWindow.Memory = TextBuffers.WindowMemory3;

        //leftWindow.Layout.WindowPadding = GuiTheme.WindowPaddingSlim;
        bottomWindow.NoBorder = true;
        //bottomWindow.BgColor = ConsolePanel.ConsoleBgColor;
        bottomWindow.Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    }


    private void RegisterPanels(ConsoleService consoleService)
    {
        RegisterPanel(new AssetListPanel(stateManager));
        RegisterPanel(new AssetInspectorPanel(stateManager));

        RegisterPanel(new SceneListPanel(stateManager));
        RegisterPanel(new SceneInspectorPanel(stateManager));

        RegisterPanel(new CameraPanel(stateManager));
        RegisterPanel(new LightingPanel(stateManager));
        RegisterPanel(new VisualPanel(stateManager));

        RegisterPanel(new ConsolePanel(stateManager, consoleService));
    }

    private void RegisterPanel<T>(T panel) where T : EditorPanel
    {
        ArgumentNullException.ThrowIfNull(panel);
        _panelDict.Add(typeof(T), panel);
    }
}
