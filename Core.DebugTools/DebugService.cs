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

public sealed class DebugService : IDisposable
{
    private readonly ImGuiController _controller;

    public DebugDataContainer Data { get; }
    public DebugTextData TextData { get; }

    public DebugConsole DevConsole { get; }
    private readonly DebugLeftPanelGui _leftPanel;
    private readonly DebugRightPanelGui _rightPanel;

    public DebugService(GL gl, IWindow window, IInputContext inputCtx)
    {
        _controller = new ImGuiController(gl, window, inputCtx);
        Data = new DebugDataContainer();
        TextData = new DebugTextData();

        DevConsole = new DebugConsole();
        _leftPanel = new DebugLeftPanelGui(TextData);
        _rightPanel = new DebugRightPanelGui(TextData);
    }

    public void RefreshSceneMetrics()
    {
        Data.SceneMetrics = DebugRouter.PullSceneMetrics?.Invoke() ?? default;
        TextData.UpdateSceneMetrics(in Data.SceneMetrics);
    }

    public void RefreshFrameMetrics()
    {
        Data.FrameMetrics = DebugRouter.PullFrameMetrics?.Invoke() ?? default;
        TextData.UpdateFrameMetrics(in Data.FrameMetrics);
    }

    public void RefreshStoreMetrics()
    {
        Data.MaterialMetrics = DebugRouter.PullMaterialMetrics?.Invoke() ?? default;
        DebugRouter.FillAssetMetrics?.Invoke(Data.AssetMetrics);
        DebugRouter.FillGfxStoreMetrics?.Invoke(Data.GfxStoreMetrics);
        
        TextData.UpdateStoreMetrics(Data);
    }

    public void RefreshMemoryMetrics()
    {
        Data.MemoryMetrics = DebugRouter.PullMemoryMetrics?.Invoke() ?? default;
        TextData.UpdateMemoryMetrics(in Data.MemoryMetrics);
    }

    private static string FormatMb(long bytes) => $"{bytes / 1024 / 1024} MB";
    private static string Format(float value) => value.ToString("0.00");


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