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

public sealed class DebugToolsSystem : IDisposable
{
    private readonly ImGuiController _controller;

    public DevConsoleService devConsole { get; }
    public MetricService Metrics { get;  }
    public EditorService Editor { get;  }
    
    private readonly DebugLeftPanelGui _leftPanel;
    private readonly DebugRightPanelGui _rightPanel;

    public DebugToolsSystem(GL gl, IWindow window, IInputContext inputCtx)
    {
        _controller = new ImGuiController(gl, window, inputCtx);

        Editor = new EditorService();
        Metrics = new MetricService();
        devConsole = new DevConsoleService();
        _leftPanel = new DebugLeftPanelGui(Metrics.TextData);
        _rightPanel = new DebugRightPanelGui(Metrics.TextData);
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
        Editor.DrawLeft();
        _leftPanel.Draw(224);
        _rightPanel.DrawRight(160);
        devConsole.Draw();
        _controller.Render();
    }
}