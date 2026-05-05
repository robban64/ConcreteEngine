using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Handles;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;

public sealed class EditorPortal : IDisposable
{
    public bool Initialized { get; private set; }
    //public bool PendingResize { get; private set; } = true;

    private EditorService _service = null!;
    
    private readonly EditorEngineContext _engineContext;

    public EditorPortal(IWindow window, EditorEngineContext engineContext, EditorEngineBundle bundle)
    {
        _engineContext = engineContext;
        EditorInputState.Input = engineContext.Input;
        
        EngineObjectStore.Create(bundle);

        ImGuiKeyMapper.Init();
        ImGuiSystem.Setup(window, 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Start(Size2D outputSize)
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        ImGuiSystem.OutputSize = outputSize;
        TextBuffers.AllocateBuffers();
        InspectorFieldProvider.Create();
        _service = new EditorService(_engineContext.GfxApi);
        Initialized = true;
        
        WindowLayout.CalculateViewport(out var vp);
        _engineContext.OnViewportChanged(vp);
    }

    public void OnResized() => _service.IsDirty = true;
    public void OnDiagnosticTick() => _service.IsDiagnosticTick = true;
    
    public MetricSystem GetMetricSystem() => MetricSystem.Instance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateInput() => ImGuiSystem.FillInput(EditorInputState.Input);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateGameTick(float deltaTime) => EditorCamera.Instance.Update(deltaTime);

    public void Render(float deltaTime, Size2D outputSize, TextureId outputTexture)
    {
        if (EditorTime.Advance(deltaTime))
            Update(outputSize, outputTexture);
        
        ImGuiSystem.RenderDrawData();
    }

    private void Update(Size2D outputSize, TextureId outputTexture)
    {
        if (ImGuiSystem.OutputSize != outputSize)
        {
            ImGuiSystem.OutputSize = outputSize;
            _service.IsDirty = true;
        }
        
        ImGuiSystem.NewFrame(EditorTime.DeltaTime, outputTexture);

        if (EditorInputState.UpdateInputState())
            EditorTime.WakeUp();

        _service.Draw();

        if (_service.IsDirty)
        {
            _service.UpdateLayout(out var vp);
            _engineContext.OnViewportChanged(vp);
            _service.IsDirty = false;
        }

        
        ImGuiSystem.EndFrame();
        _engineContext.Input.ToggleBlockInput(EditorInputState.IsBlocking);
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
        RuntimeHelpers.RunClassConstructor(typeof(ImGuiKeyMapper).TypeHandle);
    }
}