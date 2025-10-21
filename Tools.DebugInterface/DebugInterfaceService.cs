using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Tools.DebugInterface.Gui;

namespace Tools.DebugInterface;

public readonly record struct DebugGfxStoreMetric(int GfxStoreCount, int BkStoreCount, int GfxStoreFree, int BkFree);

public sealed class DebugFrameMetrics
{
    public long FrameIndex { get; set; }
    public float Fps { get; set; }
    public float Alpha { get; set; }
    public int TriangleCount { get; set; } 
    public int DrawCalls { get; set; }

}

public sealed class DebugDataContainer
{
    public DebugFrameMetrics FrameMetrics { get; init; } = new();
    public object? EntityCount { get; set; }
    public object? ShadowMapSize { get; set; }
    public object? MaterialDebugInfo { get; set; } // (int Count, int FreeSlots)
    public Dictionary<string, DebugGfxStoreMetric> GfxStoreMetrics { get; } = new(8);
    public Dictionary<string, (int, int)> AssetMetrics { get; } = new(8);
}

public sealed class DebugInterfaceService : IDisposable
{
    private readonly ImGuiController _controller;
    public DebugRegistry Registry { get; }
    public DebugDataContainer Data { get; }
    
    private readonly DebugConsoleGui _console;
    private readonly DebugLeftPanelGui _leftPanel;
    private readonly DebugRightPanelGui _rightPanel;
    
    public DebugInterfaceService(GL gl, IWindow window, IInputContext inputCtx)
    {
        _controller = new ImGuiController(gl, window, inputCtx);
        Data = new DebugDataContainer();
        Registry = new DebugRegistry();
        _console = new DebugConsoleGui();
        _leftPanel = new DebugLeftPanelGui(Data);
        _rightPanel = new DebugRightPanelGui(Data);
    }

    public void UpdateRead()
    {
        Data.EntityCount = Registry.ReadBound("EntityCount");
        Data.ShadowMapSize = Registry.ReadBound("ShadowMapSize");
        Data.MaterialDebugInfo = Registry.ReadBound("MaterialDebugInfo");
    }


    public void Dispose() => _controller.Dispose();

    public bool BlockInput()
    {
        var io = ImGui.GetIO();
        return io.WantCaptureKeyboard || io.WantCaptureMouse;
    }

    public void Update(float delta) => _controller.Update(delta);

    public void Render()
    {
        var vp = ImGui.GetMainViewport();
        _leftPanel.Draw(200);
        _rightPanel.DrawRight(200);
        _console.DrawConsole(200, 200);
        _controller.Render();
    }
}