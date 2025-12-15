using ConcreteEngine.Common;
using ConcreteEngine.Editor.Core;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;

public sealed class EditorPortal : IDisposable
{
    private readonly ImGuiController _controller;

    public bool Initialized { get; private set; }

    public bool IsMetricsMode => StateContext.ModeState.IsMetricState;

    private static bool _blockInput;

    public EditorPortal(GL gl, IWindow window, IInputContext inputCtx)
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");
        ImGuiFontConfig fontConfDefault = new(fontPath, 14);

        _controller = new ImGuiController(gl, window, inputCtx, fontConfDefault);
        inputCtx.Mice[0].Scroll += EditorInput.OnMouseScroll;
    }


    public void Initialize()
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        EditorService.Initialize();
        Initialized = true;
    }

    public bool BlockInput() => _blockInput;

    public void AddLog(string? msg) => ConsoleService.SendLog(msg);


    public void Render(float delta)
    {
        if (!Initialized) return;
        _controller.Update(delta);
        _blockInput = EditorInput.BlockInput();
        EditorInput.UpdateScroll(delta);
        EditorService.Render(delta, _blockInput);
        ImGui.Render();
        _controller.Render();
        ImGui.EndFrame();
    }


    public void Dispose()
    {
    }
}