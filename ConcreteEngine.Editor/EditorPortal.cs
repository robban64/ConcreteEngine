#region

using ConcreteEngine.Common;
using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Store;
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
        EditorManagedStore.InitFillStore();
        ModelManager.SetupModelState();
        StateContext.Init();
        Initialized = true;
    }

    public bool BlockInput() => EditorInput.BlockInput();

    public void AddLog(string? msg) => ConsoleService.SendLog(msg);

    
    public void Render(float delta)
    {
        if (!Initialized) return;
        _controller.Update(delta);
        EditorService.Render(delta, EditorInput.BlockInput());
        ImGui.Render();
        _controller.Render();
        ImGui.EndFrame();
    }


    public void Dispose()
    {
    }
}