using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;

namespace ConcreteEngine.Editor;

public sealed class EditorPortal : IDisposable
{
    public bool Initialized { get; private set; }

    private bool _isDiagnosticTick;
    private bool _wasDiagnosticTick;

    private EditorService _service = null!;


    public EditorPortal()
    {
        EditorInput.Layer = EngineInput.GetLayer(InputLayerKind.Ui);

        ImGuiKeyMapper.Init();
        ImGuiSystem.Setup(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Start()
    {
        if (Initialized) Throwers.InvalidOperation(nameof(Initialized));

        StringArena.Create();
        TextBuffers.AllocateBuffers();
        ConsoleGateway.Service.Setup();

        InspectorFieldProvider.Create();
        _service = new EditorService();
        _service.Setup();
        Initialized = true;
    }

    public void OnDiagnosticTick() => _isDiagnosticTick = true;

    public MetricSystem GetMetricSystem() => MetricSystem.Instance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateInput() => ImGuiSystem.FillInput();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateGameTick(float deltaTime) => EditorCamera.Instance.Update(deltaTime);

    public void Render(float deltaTime, TextureId outputTexture)
    {
        if(!Initialized) return;
        
        if (EditorTime.Advance(deltaTime, out var editorDelta))
            Update(editorDelta, outputTexture);

        ImGuiSystem.RenderDrawData();
    }

    private void Update(float editorDelta, TextureId outputTexture)
    {
        ImGuiSystem.NewFrame(editorDelta, outputTexture);

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

        EditorInput.ToggleBlockLayers();
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