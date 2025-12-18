using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Store;
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

    private bool _blockInput;

    public EditorPortal(GL gl, IWindow window, IInputContext inputCtx)
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");
        ImGuiFontConfig fontConfDefault = new(fontPath, 14);

        _controller = new ImGuiController(gl, window, inputCtx, fontConfDefault);
        inputCtx.Mice[0].Scroll += EditorInput.OnMouseScroll;

        EditorDataStore.ResetSlots();
        _ = ConsoleService.LogCount;
        _ = MetricsApi.CheckDelegates();
        if (EditorManagedStore.Count > 0) throw new InvalidOperationException();
        if (CommandDispatcher.HasCommands) throw new InvalidOperationException();
    }


    public void Initialize()
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        EditorService.Initialize();
        Initialized = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool BlockInput() => _blockInput;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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