using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.App;
using ConcreteEngine.Editor.App.Assets;
using ConcreteEngine.Editor.App.CLI;
using ConcreteEngine.Editor.App.Inspectors;
using ConcreteEngine.Editor.App.Scene;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.App.UI;
using ConcreteEngine.Editor.Lib;
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

        SceneWindow = new SceneWindow(stateManager);
        InspectionWindow = new InspectionWindow(stateManager);
        AssetWindow = new AssetsWindow(stateManager);
        ConsoleWindow = new ConsoleWindow(stateManager);
        _windows = [SceneWindow, InspectionWindow, AssetWindow, ConsoleWindow];

        AssetWindow.NoBorder = true;
        ConsoleWindow.NoBorder = true;
        ConsoleWindow.Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    }
    
    public void Setup()
    {
        TopMenuWindow.Instance.RegisterMenuToolbar();
        RegisterDebugWindows();

        foreach (var it in _windows) it.Create();

        TopMenuWindow.Instance.SyncToolbar();
        
        return;
        void RegisterDebugWindows()
        {
            _debugWindows[DebugMetricsWindow] = MetricsUi.Draw;
            _debugWindows[DebugImDemoWindow] = ImGui.ShowDemoWindow;
            _debugWindows[DebugImMetricsWindow] = ImGui.ShowMetricsWindow;
            _debugWindows[DebugImStyleWindow] = ImGui.ShowStyleEditor;
        }
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

        SceneWindow.Draw();
        AssetWindow.Draw();
        InspectionWindow.Draw();
        ConsoleWindow.Draw();

        if ((uint)_stateManager.ActiveDebugWindow < (uint)_debugWindows.Length)
            _debugWindows[_stateManager.ActiveDebugWindow]();
    }


}