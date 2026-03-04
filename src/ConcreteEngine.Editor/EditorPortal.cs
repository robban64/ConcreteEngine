using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGuizmo;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;

public sealed class EditorPortal : IDisposable
{
    public bool Initialized { get; private set; }

    private readonly ImGuiSystem _imguiSystem;

    private readonly GfxContext _gfxContext;

    private EditorService _service = null!;

    private bool _pendingResize = true;

    public EditorPortal(IWindow window, InputController input, GfxContext gfxContext)
    {
        _gfxContext = gfxContext;

        ImGuiKeyMapper.Init();
        StyleMap.Init();

        _imguiSystem = new ImGuiSystem(window, input);
        _imguiSystem.Setup(1);
    }

    public IMetricSystem GetMetricSystem() => MetricSystem.Instance;


    public void OnResized() => _pendingResize = true;
    public void OnTickDiagnostic() => _service.OnDiagnosticTick();

    public void Initialize(EngineController controller)
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        EngineObjects.Camera = controller.Camera;
        EngineObjects.Visuals = controller.Visuals;
        _service = new EditorService(controller, _gfxContext);
        Initialized = true;
    }

    public void Render(float delta, Size2D windowSize)
    {
        _imguiSystem.FillInput();

        if (!EditorTime.Advance(delta))
        {
            _imguiSystem.RenderDrawData();
            EditorInput.UpdateState();
            return;
        }

        _imguiSystem.NewFrame(EditorTime.DeltaTime, windowSize);

        EditorInput.UpdateState();
        if (EditorInput.IsInteracting()) EditorTime.WakeUp();

        if (_pendingResize)
        {
            _service.UpdateStyle();
            _pendingResize = false;
        }

        _service.Update();
        _service.Draw();

        _imguiSystem.EndFrame();

        _imguiSystem.RenderDrawData();
    }



    public void Dispose()
    {
        if (MetricSystem.Instance.Enabled)
        {
            /*
            var session = MetricSystem.Instance.PerfSession;
            if (session.Session.AvgMs > 0)
            {
                session.SaveSession();
                Console.WriteLine($"Performance session saved: {session.Session.AvgMs:F2}");
            }
            */
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
        RuntimeHelpers.RunClassConstructor(typeof(CommandDispatcher).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EditorInput).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(GuiTheme).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(Palette).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(StyleMap).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(ImGuiKeyMapper).TypeHandle);
    }
}