using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Components.Layout;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;

public sealed class EditorPortal : IDisposable
{
    public bool Initialized { get; private set; }
    public bool BlockInput { get; private set; }

    private readonly ImGuiRenderer _renderer;

    //private static RefreshRateController _rateController = null!;

    public EditorPortal(IWindow window, EditorInputSource source)
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");

        _renderer = new  ImGuiRenderer(window,source);
        _renderer.Setup(fontPath, 1);
    }


    public bool IsMetricsMode => StateContext.ModeState.IsMetricState;

    public void Initialize()
    {

        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        EditorService.Initialize();
        Initialized = true;
    }

    public void MainRender(float deltaTime, Size2D windowSize)
    {
        _renderer.BeginFrame(deltaTime, windowSize);        
        ImGui.ShowDemoWindow();
        EditorService.Render(deltaTime, BlockInput);
        ImGui.Render();

        _renderer.EndFrame();
    }
/*
    public void Render(float delta)
    {
        _rateController.AddDelta(delta);
        if (_rateController.ShouldUpdate(out var step))
        {
            _controller.Update(step);

            if (EditorInput.IsInteracting()) _rateController.WakeUp();
            BlockInput = EditorInput.BlockInput();
            EditorInput.UpdateScroll();

            EditorService.Render(step, BlockInput);

            ImGui.Render();

            _rateController.EndUpdate();
        }

        _rateController.Draw();
    }*/

    public void OnTickDiagnostic()
    {
        ConsoleGateway.OnTick();

        if (StateContext.ModeState.IsMetricState)
        {
            MetricsApi.Tick();
        }
    }


    public void Dispose()
    {
        if (MetricsApi.HasInitialized && MetricsApi.Enabled)
        {
            var session = MetricsApi.GetPerformanceSession();
            if (session.Session.AvgMs > 0) session.SaveSession();
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
        EditorDataStore.WarmUp();
    }

    public static void RunStaticCtor()
    {
        Type[] types =
        [
            typeof(ManagedStore),
            typeof(ConsoleGateway),
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