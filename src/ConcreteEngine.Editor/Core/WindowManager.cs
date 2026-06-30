using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedVariable

namespace ConcreteEngine.Editor.Core;

internal sealed class WindowManager
{
    public const int DebugWindowCount = 4;

    public const int DebugMetricsWindow = 0;
    public const int DebugImDemoWindow = 1;
    public const int DebugImMetricsWindow = 2;
    public const int DebugImStyleWindow = 3;

    //
    private readonly StateManager _stateManager;

    private readonly EditorWindow[] _windows;
    private readonly Action[] _debugWindows;

    public readonly SceneWindow SceneWindow;
    public readonly InspectionWindow InspectionWindow;
    public readonly AssetsWindow AssetWindow;
    public readonly ConsoleWindow ConsoleWindow;

    public WindowManager(StateManager stateManager)
    {
        _stateManager = stateManager;
        _debugWindows = new Action[DebugWindowCount];

        var sceneWindow = SceneWindow = new SceneWindow(stateManager);
        var inspectionWindow = InspectionWindow = new InspectionWindow(stateManager);
        var assetWindow = AssetWindow = new AssetsWindow(stateManager);
        var consoleWindow = ConsoleWindow = new ConsoleWindow(stateManager);
        _windows = [sceneWindow, inspectionWindow, assetWindow, consoleWindow];

        consoleWindow.NoBorder = true;
        consoleWindow.Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        consoleWindow.WindowPadding = GuiTheme.WindowPadding with { Y = 1f };

        assetWindow.NoBorder = true;
        //assetWindow.WindowPadding = GuiTheme.WindowPadding with { Y = 0f };
    }

    public EditorWindow GetWindow(WindowId windowId) => _windows[(int)windowId];

    public T GetWindow<T>() where T : EditorWindow
    {
        foreach (var it in _windows)
        {
            if (it is T window) return window;
        }

        Throwers.InvalidArgument(nameof(T));
        return null;
    }

    public void OnDiagnosticTick()
    {
        foreach (var window in _windows)
        {
            if (window.Enabled) window.OnUpdateDiagnostic();
        }
    }

    public void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        WindowRoot.BeginDockSpace();
        ViewportWindow.Draw(_stateManager);
        ImGui.PopStyleVar();

        TopMenuWindow.Instance.Draw(_stateManager);

        foreach (var window in _windows) window.Draw();

        if ((uint)_stateManager.ActiveDebugWindow < (uint)_debugWindows.Length)
            _debugWindows[_stateManager.ActiveDebugWindow]();
    }


    public void Setup()
    {
        TopMenuWindow.Instance.RegisterMenuToolbar();
        RegisterDebugWindows();

        foreach (var it in _windows) it.Create();

        TopMenuWindow.Instance.SyncToolbar();
    }


    private void RegisterDebugWindows()
    {
        _debugWindows[DebugMetricsWindow] = MetricsUi.Draw;
        _debugWindows[DebugImDemoWindow] = ImGui.ShowDemoWindow;
        _debugWindows[DebugImMetricsWindow] = ImGui.ShowMetricsWindow;
        _debugWindows[DebugImStyleWindow] = ImGui.ShowStyleEditor;
    }
}