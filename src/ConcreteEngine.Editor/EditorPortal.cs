using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Handles;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;

public sealed class EditorPortal : IDisposable
{
    public bool Initialized { get; private set; }

    private bool _isDiagnosticTick;
    private bool _wasDiagnosticTick;

    private EditorService _service = null!;

    private readonly EditorEngineContext _engineContext;

    public EditorPortal(IWindow window, EditorEngineContext engineContext, EditorEngineBundle bundle)
    {
        _engineContext = engineContext;
        EditorInput.Input = engineContext.Input;

        EngineObjectStore.Create(bundle);

        ImGuiKeyMapper.Init();
        ImGuiSystem.Setup(window, 1);

        WindowRoot.OnViewport = engineContext.OnViewportChanged;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Start(Size2D outputSize)
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));

        ImGuiSystem.OutputSize = outputSize;
        TextBuffers.AllocateBuffers();
        ConsoleGateway.Service.Setup();

        InspectorFieldProvider.Create();
        _service = new EditorService(_engineContext.GfxApi);
        Initialized = true;
    }

    public void OnDiagnosticTick() => _isDiagnosticTick = true;

    public MetricSystem GetMetricSystem() => MetricSystem.Instance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateInput() => ImGuiSystem.FillInput(EditorInput.Input);

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
        ImGuiSystem.NewFrame(EditorTime.DeltaTime, outputSize, outputTexture);

        if (_isDiagnosticTick)
        {
            _isDiagnosticTick = false;
            _wasDiagnosticTick = true;
            ConsoleGateway.Service.OnTick();
        }

        if (_wasDiagnosticTick)
        {
            _wasDiagnosticTick = false;
            _service.OnDiagnosticTick();
        }
        _service.Draw();

        ImGuiSystem.EndFrame();

        _engineContext.Input.ToggleBlockInput(EditorInput.IsBlocking);
    }


    public void Dispose()
    {
        TextBuffers.Dispose();

        ImGuiImplOpenGL3.Shutdown();
        ImGuiImplOpenGL3.SetCurrentContext(null);
        ImGuiImplGLFW.Shutdown();
        ImGuiImplGLFW.SetCurrentContext(null);
        ImGui.DestroyContext();
    }
/*
    public static ViewportRect PredictInitialViewport(Size2D outputSize)
    {
        float width = outputSize.Width * (1.0f - 0.20f - 0.20f);
        float height = outputSize.Height * (1.0f - 0.25f) - GuiTheme.TopOffset;

        float posX = outputSize.Width * 0.20f;
        float posY = GuiTheme.TopOffset;

        return new ViewportRect(new Vector2I(width, height), new Size2D(posX, posY));
    }
*/

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RunStaticCtor()
    {
        RuntimeHelpers.RunClassConstructor(typeof(ConsoleGateway).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(CommandDispatcher).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EditorInput).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(GuiTheme).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(Palette).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(ImGuiKeyMapper).TypeHandle);
    }
}