using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Tools.DebugInterface.Components;
using Tools.DebugInterface.Data;

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
    }

    public void UpdateSlowRead1()
    {
        var matInfo = Registry.ReadBound("MaterialDebugInfo");
        Data.MaterialDebugInfo = $"Materials: {matInfo}";
    }

    public void UpdateSlowRead2()
    {
        Data.FrameMetrics.Allocated = $"Allocated: {FormatMb(GC.GetAllocatedBytesForCurrentThread())}";
    }

    private static string FormatMb(long bytes) => $"{bytes / 1024 / 1024} MB";


    public void Dispose() => _controller.Dispose();

    public bool BlockInput()
    {
        var io = ImGui.GetIO();

        var blockKeyboard = io.WantTextInput || ImGui.IsAnyItemActive() || ImGui.IsAnyItemFocused();

        var anyMouseDown = io.MouseDown[0] || io.MouseDown[1] || io.MouseDown[2] || io.MouseDown[3] || io.MouseDown[4];

        var overUi = ImGui.IsAnyItemHovered() || ImGui.IsAnyItemActive() ||
                     ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);

        var blockMouse = anyMouseDown && overUi;

        if (ImGui.IsPopupOpen(null, ImGuiPopupFlags.AnyPopupId))
            blockMouse |= ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);

        return blockKeyboard || blockMouse;
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