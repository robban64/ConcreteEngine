using System.Numerics;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer.State;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace ConcreteEngine.Core.Interface;

public record struct ImGuiStats(
    in RenderFrameInfo RenderInfo,
    in RenderRuntimeParams RenderParams,
    in GfxFrameResult FrameResult,
    int Entities);

internal sealed class ImGuiSystem : IDisposable
{
    private readonly ImGuiController _controller;
    private readonly IWindow _window;
    
    private bool _showMetrics = true;
    
    private ImGuiStats _nextStats;
    private ImGuiStats _currentStats;

    private ImGuiIOPtr Io => ImGui.GetIO();

    public ImGuiSystem(GL gl, IWindow window, IInputContext inputCtx)
    {
        _window = window;
        _controller = new ImGuiController(gl, window, inputCtx);
    }

    public bool BlockInput()
    {
        var io = Io;
        return io.WantCaptureKeyboard || io.WantCaptureMouse;
    }

    public void Update(float delta) => _controller.Update(delta);

    public void RefreshStats() => _currentStats = _nextStats;

    public void Render(in ImGuiStats stats)
    {
        _nextStats = stats;
        
        ImGui.SetNextWindowPos(_currentStats.RenderParams.MousePos, ImGuiCond.FirstUseEver, Vector2.Zero);
        ImGui.SetNextWindowSize(new Vector2(400, 0), ImGuiCond.FirstUseEver); // auto height
        var flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
                    ImGuiWindowFlags.AlwaysAutoResize;

        if (ImGui.Begin("Metrics", ref _showMetrics, flags))
        {
            ImGui.Text($"FPS: {Format(_currentStats.RenderInfo.Fps)}");
            ImGui.Text($"Frame Index: {_currentStats.RenderInfo.FrameIndex} ms");
            ImGui.Text($"Delta Time: {Format(_currentStats.RenderInfo.DeltaTime)} ms");
            ImGui.Separator();
            ImGui.Text($"Verts: {_currentStats.FrameResult.TriangleCount}");
            ImGui.Text($"Draws: {_currentStats.FrameResult.DrawCalls}");
            ImGui.Separator();
            ImGui.Text($"Entities: {_currentStats.Entities}");
        }

        ImGui.End();

        _controller.Render();
    }

    public void Dispose() => _controller.Dispose();
    
    private static string Format(float value) => value.ToString("0.00"); 
}