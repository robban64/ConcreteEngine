using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;

public sealed class EditorPortal : IDisposable
{
    public bool Initialized { get; private set; }

    private readonly ImGuiController _controller;

    private readonly GfxContext _gfxContext;
    private EditorService _service = null!;

    private RefreshRateTicker _rateTicker;

    private bool _pendingResize = true;

    public EditorPortal(IWindow window, InputController input, GfxContext gfxContext)
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Content", "lucide.ttf");

        _gfxContext = gfxContext;

        ImGuiKeyMapper.Init();
        StyleMap.Init();

        _rateTicker = RefreshRateTicker.Make();
        _controller = new ImGuiController(window, input);
        _controller.Setup(fontPath, iconPath, 1);
    }


    public void OnResized() => _pendingResize = true;
    public void OnTickDiagnostic() => _service.OnDiagnosticTick();

    public void Initialize(EngineController controller)
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        _service = new EditorService(controller, _gfxContext);
        Initialized = true;
    }

    public void Render(float delta, Size2D windowSize)
    {
        _controller.UpdateInputChar();

        if (!_rateTicker.Accumulate(delta, out var step))
        {
            _controller.RenderDrawData();
            EditorInput.UpdateState();
            return;
        }

        _controller.NewFrame(step, windowSize);

        EditorInput.UpdateState();
        if (EditorInput.IsInteracting()) _rateTicker.WakeUp();

        if (_pendingResize)
        {
            _service.UpdateStyle();
            _pendingResize = false;
        }

        _service.Render(step);

        _controller.EndFrame();

        _controller.RenderDrawData();
    }


    public void Dispose()
    {
        if (MetricsApi.HasInitialized && MetricsApi.Enabled)
        {
            var session = MetricsApi.GetPerformanceSession();
            if (session.Session.AvgMs > 0)
            {
                session.SaveSession();
                Console.WriteLine($"Performance session saved: {session.Session.AvgMs:F2}");
            }
        }

        ImGuiImplOpenGL3.Shutdown();
        ImGuiImplOpenGL3.SetCurrentContext(null);
        ImGuiImplGLFW.Shutdown();
        ImGuiImplGLFW.SetCurrentContext(null);
        ImGui.DestroyContext();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]    
    public static void RunStaticCtor()
    {
        RuntimeHelpers.RunClassConstructor(typeof(ConsoleGateway).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(MetricsApi).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(CommandDispatcher).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EditorInput).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(GuiTheme).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(Palette).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(StyleMap).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(ImGuiKeyMapper).TypeHandle);
    }
}
