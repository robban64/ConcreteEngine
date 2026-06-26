using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Visuals;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedVariable

namespace ConcreteEngine.Editor.Core;

internal sealed class WindowManager
{
    private const int WindowCount = 4;
    public const int DebugWindowCount = 4;

    public const int DebugMetricsWindow = 0;
    public const int DebugImDemoWindow = 1;
    public const int DebugImMetricsWindow = 2;
    public const int DebugImStyleWindow = 3;

    //
    private readonly StateManager _stateManager;

    private readonly SceneWindow _sceneWindow;
    private readonly InspectionWindow _inspectionWindow;
    private readonly AssetsWindow _assetWindow;
    private readonly ConsoleWindow _consoleWindow;

    private readonly EditorWindow[] _windows;
    private readonly Action[] _debugWindows = new Action[DebugWindowCount];

    public WindowManager(StateManager stateManager)
    {
        _stateManager = stateManager;
        var sceneWindow = _sceneWindow = new SceneWindow(stateManager);
        var inspectionWindow = _inspectionWindow = new InspectionWindow(stateManager);
        var assetWindow = _assetWindow = new AssetsWindow(stateManager);
        var consoleWindow = _consoleWindow = new ConsoleWindow(stateManager);
        _windows = [sceneWindow, inspectionWindow, assetWindow, consoleWindow];

        consoleWindow.NoBorder = true;
        consoleWindow.Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    }

    public EditorWindow GetWindow(WindowId windowId) => _windows[(int)windowId];

    public void OnDiagnosticTick()
    {
        foreach (var window in _windows)
            window.OnUpdateDiagnostic();
    }

    public void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        if (WindowRoot.BeginDockSpace())
            EngineWindow.SetViewport(new ViewportRect(WindowRoot.ViewportPosition, WindowRoot.ViewportSize));

        ViewportWindow.Draw(_stateManager);
        ImGui.PopStyleVar();

        TopMenuWindow.DrawMenu(_stateManager);
        TopMenuWindow.DrawToolbar(_stateManager);

        foreach (var window in _windows)
            window.Draw();

        if ((uint)_stateManager.ActiveDebugWindow < (uint)_debugWindows.Length)
            _debugWindows[_stateManager.ActiveDebugWindow]();
    }


    public void Init(ArenaAllocator allocator)
    {
        TopMenuWindow.RegisterMenuToolbar(allocator);
        RegisterDebugWindows();

        foreach (var it in _windows)
            it.OnCreate();

        TopMenuWindow.SyncToolbar();
    }


    private void RegisterDebugWindows()
    {
        _debugWindows[DebugMetricsWindow] = MetricsUi.Draw;
        _debugWindows[DebugImDemoWindow] = ImGui.ShowDemoWindow;
        _debugWindows[DebugImMetricsWindow] = ImGui.ShowMetricsWindow;
        _debugWindows[DebugImStyleWindow] = ImGui.ShowStyleEditor;
    }
}