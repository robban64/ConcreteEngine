using ConcreteEngine.Editor.App;
using ConcreteEngine.Editor.App.Assets;
using ConcreteEngine.Editor.App.CLI;
using ConcreteEngine.Editor.App.Inspectors;
using ConcreteEngine.Editor.App.Scene;
using ConcreteEngine.Editor.App.UI;
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

        AssetWindow.NoBorder = true;
        ConsoleWindow.NoBorder = true;
        ConsoleWindow.Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    }

    public void Setup()
    {
        TopMenuWindow.Create();
        RegisterDebugWindows();

        SceneWindow.Create();
        AssetWindow.Create();
        InspectionWindow.Create();
        ConsoleWindow.Create();

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


    public void OnDiagnosticTick()
    {
        if (SceneWindow.Enabled) SceneWindow.OnUpdateDiagnostic();
        if (AssetWindow.Enabled) AssetWindow.OnUpdateDiagnostic();
        if (InspectionWindow.Enabled) InspectionWindow.OnUpdateDiagnostic();
        if (ConsoleWindow.Enabled) ConsoleWindow.OnUpdateDiagnostic();
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