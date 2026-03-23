using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
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

    private readonly GfxContext _gfxContext;

    private EditorService _service = null!;

    private bool _pendingResize = true;

    public EditorPortal(IWindow window, InputController input, GfxContext gfxContext)
    {
        _gfxContext = gfxContext;

        ImGuiKeyMapper.Init();
        StyleMap.Init();

        EditorInputState.Input = input;

        ImGuiSystem.Setup(window, 1);
    }

    public MetricSystem GetMetricSystem() => MetricSystem.Instance;

    public void OnResized() => _pendingResize = true;

    public void Initialize(EngineController controller)
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        EngineObjectStore.Init(controller);
        _service = new EditorService(_gfxContext);
        Initialized = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateDiagnostic() => _service.OnDiagnosticTick();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateInput() => ImGuiSystem.FillInput(EditorInputState.Input);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateGameTick(float deltaTime) => EditorCamera.Instance.Update(deltaTime);

    public void Render(float deltaTime, Size2D windowSize)
    {
        if (!EditorTime.Advance(deltaTime))
        {
            ImGuiSystem.RenderDrawData();
            return;
        }

        ImGuiSystem.NewFrame(EditorTime.DeltaTime, windowSize);

        if (_pendingResize)
        {
            _service.UpdateStyle();
            _pendingResize = false;
        }

        if (EditorInputState.UpdateInputState())
            EditorTime.WakeUp();

        _service.Draw();

        ImGuiSystem.EndFrame();
        ImGuiSystem.RenderDrawData();

        EditorInputState.UpdateInputBlock();
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

        TextBuffers.Dispose();
        StyleMap.Dispose();
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
        RuntimeHelpers.RunClassConstructor(typeof(EditorInputState).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(GuiTheme).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(Palette).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(StyleMap).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(ImGuiKeyMapper).TypeHandle);
    }
}