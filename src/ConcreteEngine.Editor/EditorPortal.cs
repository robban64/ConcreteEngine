using System.Reflection;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Components.Layout;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
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

    private static ImGuiController _controller = null!;
    private static RefreshRateController _rateController = null!;

    public EditorPortal(in EditorPortalArgs args)
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");
        ImGuiFontConfig fontConfDefault = new(fontPath, 14);

        _controller = new ImGuiController(args.Gl, args.Window, args.InputCtx, fontConfDefault);
        _rateController = new RefreshRateController(_controller);

        args.InputCtx.Mice[0].Scroll += static (_, wheel) =>
        {
            EditorInput.OnMouseScroll(_, wheel);
            _rateController.WakeUp(); 
        };
        WarmUp();
    }

    public bool IsMetricsMode => StateContext.ModeState.IsMetricState;

    public void Initialize()
    {
        InvalidOpThrower.ThrowIf(Initialized, nameof(Initialized));
        EditorService.Initialize();
        Initialized = true;
    }

    public void Render(float delta)
    {
        _rateController.AddDelta(delta);
        if (_rateController.ShouldUpdate(out float step))
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
    }

    public void OnTickDiagnostic()
    {
        ConsoleGateway.Context.FlushLogQueue();

        if (StateContext.ModeState.IsMetricState)
            MetricsApi.Tick();
    }


    public void Dispose()
    {
        _controller.Dispose();
        _controller = null!;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WarmUp()
    {
        var types = GetStaticCtorTypes();
        foreach (var it in types)
            RuntimeHelpers.RunClassConstructor(it.TypeHandle);

        EditorDataStore.WarmUp();
    }

    private static Type[] GetStaticCtorTypes() =>
    [
        typeof(ManagedStore),
        typeof(EditorDataStore),
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