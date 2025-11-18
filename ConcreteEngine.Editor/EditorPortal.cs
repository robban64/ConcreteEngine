#region

using ConcreteEngine.Common;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

#endregion

namespace ConcreteEngine.Editor;

public sealed class EditorPortal : IDisposable
{
    private readonly ImGuiController _controller;

    public bool Initialized { get; private set; } = false;

    public EditorPortal(GL gl, IWindow window, IInputContext inputCtx)
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");
        ImGuiFontConfig fontConfDefault = new(fontPath, 14);

        _controller = new ImGuiController(gl, window, inputCtx, fontConfDefault);
    }

    public void Initialize()
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        ModelManager.SetupModelState();
        StateContext.Init();
        Initialized = true;
    }

    public void AddLog(string? msg) => ConsoleService.SendLog(msg);


    public void Update(float delta)
    {
        if (!Initialized) return;
        _controller.Update(delta);
        EditorService.Render(delta, BlockInput());
        ImGui.Render();
    }

    public void Render()
    {
        if (!Initialized) return;
        _controller.Render();
    }

    //TODO proper input

 
    public bool BlockInput()
    {
        var io = ImGui.GetIO();

        var blockKeyboard = io.WantTextInput || io.WantCaptureKeyboard || ImGui.IsAnyItemActive() ||
                            ImGui.IsAnyItemFocused();

        //var anyMouseDown = io.MouseDown[0] || io.MouseDown[1] || io.MouseDown[2] || io.MouseDown[3] || io.MouseDown[4];
        var overUi = ImGui.IsAnyItemHovered() || ImGui.IsAnyItemActive() ||
                     ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);

        var blockMouse = ImGui.IsAnyMouseDown() && overUi;

        if (ImGui.IsPopupOpen(null, ImGuiPopupFlags.AnyPopupId))
            blockMouse |= ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);

        return blockKeyboard || blockMouse;
    }

    /*
    public bool BlockInput()
    {
        var io = ImGui.GetIO();

        var blockKeyboard = io.WantCaptureKeyboard || io.WantTextInput || ImGui.IsAnyItemActive() ||
                            ImGui.IsAnyItemFocused();

        if (io.WantCaptureMouse)
            return blockKeyboard || true;

        var anyMouseDown = io.MouseDown[0] || io.MouseDown[1] || io.MouseDown[2];
        var mouseOverAnyWindow = ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);

        var blockMouse = anyMouseDown && mouseOverAnyWindow;

        return blockKeyboard || blockMouse;
    }
    */

    public void Dispose()
    {
    }
}