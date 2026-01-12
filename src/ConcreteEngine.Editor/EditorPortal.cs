using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;

public sealed class EditorPortal : IDisposable
{
    public bool Initialized { get; private set; }

    private readonly ImGuiController _controller;
    private readonly InputController _input;
    private readonly RefreshRateController _rateController;

    private readonly EditorService _service;

    public EditorPortal(IWindow window, InputController input)
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");

        ImGuiKeyMapper.Init();

        _input = input;
        _service = new EditorService();
        _rateController = new RefreshRateController();
        _controller = new ImGuiController(window, input);
        _controller.Setup(fontPath, 1);
    }


    public void OnResized() => _service.RefreshStyle();

    public void Initialize()
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        _service.Initialize();
        Initialized = true;
    }

    public void MainRender(float delta, Size2D windowSize)
    {
        _controller.UpdateInputChar();

        _rateController.AddDelta(delta);

        if (_rateController.ShouldUpdate(out var step))
        {
            _controller.SetFrameData(step, windowSize);
            _controller.NewFrame();

            if (EditorInput.IsInteracting()) _rateController.WakeUp();

            _service.Render(step);
            _controller.EndFrame();
        }

        _controller.RenderDrawData();
    }

    public void OnTickDiagnostic() => _service.OnDiagnosticTick();


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


    public static void RunStaticCtor()
    {
        RuntimeHelpers.RunClassConstructor(typeof(ConsoleGateway).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(MetricsApi).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(CommandDispatcher).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EditorInput).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(GuiTheme).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(StrUtils).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(ConsoleComponent).TypeHandle);
    }
}

/*
      public void Render2(float delta)
      {
          _ticker.Accumulate(delta);

          if (_ticker.DequeueTick())
          {
              _controller.Update(UiDelta);

              EditorInput.UpdateScroll();
              _blockInput = EditorInput.BlockInput();
              EditorService.Render(UiDelta, _blockInput);

              ImGui.Render();

              _lastDrawData = ImGui.GetDrawData();
              _hasRenderedOnce = true;
          }

          if (_hasRenderedOnce)
          {
              _drawBinding(_lastDrawData);
          }
      }


      public void RenderFast(float delta)
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
  */