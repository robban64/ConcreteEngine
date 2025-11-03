#region

using Core.DebugTools.Components;
using Core.DebugTools.Data;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

#endregion

namespace Core.DebugTools;

public sealed class DiagnosticsService : IDisposable
{
    private readonly ImGuiController _controller;

    public MetricData Data { get; }
    public MetricReport TextData { get; }

    public DebugConsole DevConsole { get; }
    private readonly DebugLeftPanelGui _leftPanel;
    private readonly DebugRightPanelGui _rightPanel;

    public DiagnosticsService(GL gl, IWindow window, IInputContext inputCtx)
    {
        _controller = new ImGuiController(gl, window, inputCtx);
        Data = new MetricData();
        TextData = new MetricReport();

        DevConsole = new DebugConsole();
        _leftPanel = new DebugLeftPanelGui(TextData);
        _rightPanel = new DebugRightPanelGui(TextData);
    }

    public void RefreshSceneMetrics()
    {
        Data.SceneMetrics = RouteTable.PullSceneMetrics?.Invoke() ?? default;
        TextData.UpdateSceneMetrics(in Data.SceneMetrics);
    }

    public void RefreshFrameMetrics()
    {
        Data.FrameMetrics = RouteTable.PullFrameMetrics?.Invoke() ?? default;
        TextData.UpdateFrameMetrics(in Data.FrameMetrics);
    }

    public void RefreshStoreMetrics()
    {
        Data.MaterialMetrics = RouteTable.PullMaterialMetrics?.Invoke() ?? default;
        TextData.UpdateMaterialMetrics(in Data.MaterialMetrics);

        RouteTable.FillAssetMetrics?.Invoke(Data);
        RouteTable.FillGfxStoreMetrics?.Invoke(Data);
        TextData.UpdateAssetMetrics(Data.AssetMetrics);
        TextData.UpdateGfxStoreMetrics(Data.GfxStoreMetrics);
    }

    public void RefreshMemoryMetrics()
    {
        Data.MemoryMetrics = RouteTable.PullMemoryMetrics?.Invoke() ?? default;
        TextData.UpdateMemoryMetrics(Data.MemoryMetrics);
    }

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
        ImGui.ShowDemoWindow();
        _leftPanel.Draw(224);
        _rightPanel.DrawRight(160);
        DevConsole.DrawConsole(200, 200);
        _controller.Render();
    }
}