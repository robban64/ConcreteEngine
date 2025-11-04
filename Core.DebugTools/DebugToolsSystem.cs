#region

using Core.DebugTools.Data;
using Core.DebugTools.Gui;
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

    public DebugToolsSystem(GL gl, IWindow window, IInputContext inputCtx)
    {
        _controller = new ImGuiController(gl, window, inputCtx);

        Metrics = new MetricService();
        devConsole = new DevConsoleService();
        Editor = new EditorService(Metrics);
    }

    public void Update(float delta) => _controller.Update(delta);

    public void Render()
    {
        var vp = ImGui.GetMainViewport();
        Editor.Render();
        devConsole.Draw();
        _controller.Render();
    }

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

    public void Dispose() => _controller.Dispose();

}