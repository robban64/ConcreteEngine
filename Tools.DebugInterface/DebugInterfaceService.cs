using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Tools.DebugInterface.Components;

namespace Tools.DebugInterface;

public sealed class DebugInterfaceService : IDisposable
{
    private readonly ImGuiController _controller;
    public DebugRegistry Registry { get; }
    public DebugDataContainer Data { get; }

    public DebugConsole DevConsole { get; }
    private readonly DebugLeftPanelGui _leftPanel;
    private readonly DebugRightPanelGui _rightPanel;

    public DebugInterfaceService(GL gl, IWindow window, IInputContext inputCtx)
    {
        _controller = new ImGuiController(gl, window, inputCtx);
        Data = new DebugDataContainer();
        Registry = new DebugRegistry();
        DevConsole = new DebugConsole();
        _leftPanel = new DebugLeftPanelGui(Data);
        _rightPanel = new DebugRightPanelGui(Data);
    }

    public void UpdateRead()
    {
        var entityCount = Registry.ReadBound("EntityCount");
        Data.EntityCount = $"Entities: {entityCount}";

        var shadowSize = Registry.ReadBound("ShadowMapSize");
        Data.ShadowMapSize = $"ShadowMapSize: {shadowSize}";

        var matInfo = Registry.ReadBound("MaterialDebugInfo");
        Data.MaterialDebugInfo = $"Materials: {matInfo}";

        var metrics = Data.MemoryMetrics;
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var gcInfo = GC.GetGCMemoryInfo();

        metrics.GcGen = $"GC Gen: {GC.CollectionCount(0)}, {GC.CollectionCount(1)}, {GC.CollectionCount(1)}";
        metrics.TotalMemory = $"AppMemory: {FormatMb(GC.GetTotalMemory(false))}";
        metrics.Allocated   = $"Allocated: {FormatMb(allocated)}";
        metrics.HeapSize    = $"Heap Size: {FormatMb(gcInfo.HeapSizeBytes)}";
    }

    private static string FormatMb(long bytes) => $"{bytes / 1024 / 1024} MB";


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
        DevConsole.DrawConsole(200, 200);
        _controller.Render();
    }
}