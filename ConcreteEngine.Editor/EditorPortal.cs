using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Components.Layout;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;

public readonly ref struct EditorPortalArgs(GL gl, IWindow window, IInputContext inputCtx)
{
    public readonly GL Gl = gl;
    public readonly IWindow Window = window;
    public readonly IInputContext InputCtx = inputCtx;
}

public sealed class EditorPortal : IDisposable
{
    private readonly ImGuiController _controller;

    public bool Initialized { get; private set; }

    public bool IsMetricsMode => StateContext.ModeState.IsMetricState;

    private bool _blockInput;

    public EditorPortal(in EditorPortalArgs args)
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");
        ImGuiFontConfig fontConfDefault = new(fontPath, 14);

        _controller = new ImGuiController(args.Gl, args.Window, args.InputCtx, fontConfDefault);
        args.InputCtx.Mice[0].Scroll += EditorInput.OnMouseScroll;
        WarmUp();
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


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WarmUp()
    {
        EditorDataStore.WarmUp();

        var types = GetStaticCtorTypes();
        foreach (var it in types)
            RuntimeHelpers.RunClassConstructor(it.TypeHandle);

    }

    private static Type[] GetStaticCtorTypes() =>
    [
        typeof(ManagedStore),
        typeof(ManagedStore),
        typeof(ConsoleService),
        typeof(EditorApi),
        typeof(MetricsApi),
        typeof(CommandDispatcher),
        typeof(ModelManager),
        typeof(CommandDispatcher),
        typeof(EditorService),
        typeof(StateContext),
        typeof(EditorInput),
        typeof(GuiTheme),
        typeof(StringUtils),
        typeof(AssetsComponent),
        typeof(CameraComponent),
        typeof(ConsoleComponent),
        typeof(EntitiesComponent),
        typeof(WorldParamsComponent),
        typeof(Topbar)
    ];
}