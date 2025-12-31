using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
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
using Hexa.NET.ImGuizmo;
using Silk.NET.Input;
using Silk.NET.OpenGL;
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
    public bool Initialized { get; private set; }
    public bool BlockInput { get; private set; }

    private IWindow _window;

    //private static RefreshRateController _rateController = null!;

    private ImGuiContextPtr _imGuiContext;

    private GL _gl;

    public unsafe EditorPortal(in EditorPortalArgs args)
    {
        _gl = args.Gl;
        _window = args.Window;

        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");
        //ImGuiFontConfig fontConfDefault = new(fontPath, 14);

        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGuiImplGLFW.SetCurrentContext(_imGuiContext);

        
        var windowPtr = (GLFWwindow*)args.Window.Handle;
        ImGuiImplOpenGL3.SetCurrentContext(_imGuiContext);
        ImGuiImplOpenGL3.Init("#version 330");
        ImGuiImplGLFW.InitForOpenGL(windowPtr, true);

        io.DisplaySize = (Vector2)args.Window.Size;
        io.DisplayFramebufferScale = Vector2.One;


        ImGui.StyleColorsDark();
        var style = ImGui.GetStyle();
        //style.ScaleAllSizes(12);
        
        /*
        _rateController = new RefreshRateController(_controller);
        args.InputCtx.Mice[0].Scroll += static (_, wheel) =>
        {
            EditorInput.OnMouseScroll(_, wheel);
            _rateController.WakeUp();
        };*/
    }

    private void LoadImGui()
    {
    }

    public bool IsMetricsMode => StateContext.ModeState.IsMetricState;

    public void Initialize()
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        EditorService.Initialize();
        Initialized = true;
    }

    public void MainRender(float delta)
    {
        ImGuiImplOpenGL3.NewFrame();
        ImGuiImplGLFW.NewFrame();
        
        var io = ImGui.GetIO();
        io.DisplaySize = (Vector2)_window.Size;
        io.DisplayFramebufferScale = Vector2.One;
        io.DeltaTime = delta;
        
        
        ImGui.NewFrame();


        ImGui.ShowDemoWindow();
        EditorService.Render(delta, BlockInput);

        ImGui.Render();

        _gl.BindVertexArray(0);
        
        if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
            _window.MakeCurrent();
        }
        
        var drawData = ImGui.GetDrawData();
        if (drawData.DisplaySize.X > 0 && drawData.DisplaySize.Y > 0)
        {
            _gl.BindVertexArray(0);
            ImGuiImplOpenGL3.RenderDrawData(drawData);
        }
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