using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Components.Layout;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
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
    private readonly EditorEngineController _engine;

    private readonly RefreshRateController _rateController;


    public EditorPortal(IWindow window, EditorEngineController engine)
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");

        ImGuiKeyMapper.Init();

        _engine = engine;
        _rateController = new RefreshRateController();
        _controller = new ImGuiController(window, engine);
        _controller.Setup(fontPath, 1);
    }


    public static void OnResized() => EditorService.RefreshStyle();

    public void Initialize()
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        EditorService.Initialize();
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

            EditorService.Render(step);

            _controller.EndFrame();
        }

        _controller.RenderDrawData();
    }

    public void OnTickDiagnostic()
    {
        ConsoleGateway.OnTick();

        if (StateManager.ModeState.IsMetricsMode)
        {
            MetricsApi.Tick();
        }
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
    public static void WarmUp()
    {
        StoreHub.WarmUp();
    }

    public static void RunStaticCtor()
    {
        Type[] types =
        [
            typeof(ConsoleGateway),
            typeof(MetricsApi),
            typeof(CommandDispatcher),
            typeof(ModelManager),
            typeof(CommandDispatcher),
            typeof(EditorService),
            typeof(StateManager),
            typeof(EditorInput),
            typeof(GuiTheme),
            typeof(StrUtils),
            typeof(AssetsComponent),
            typeof(CameraComponent),
            typeof(ConsoleComponent),
            typeof(VisualParamComponent),
            typeof(Topbar)
        ];
        foreach (var it in types)
        {
            RuntimeHelpers.RunClassConstructor(it.TypeHandle);
        }

        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<EventKey>).TypeHandle);
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