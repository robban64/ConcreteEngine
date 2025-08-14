#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Input;
using ConcreteEngine.Core.Pipeline;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.OpenGL;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Shader = ConcreteEngine.Core.Assets.Shader;

#endregion

namespace ConcreteEngine.Core;

public sealed class GameEngine: IDisposable
{
    private readonly IWindow _window;
    private readonly GameProgram _boundProgram;
    
    private IGraphicsDevice _graphics  = null!;
    private InputManager _input = null!;
    private AssetManager _assets = null!;
    private RenderPipeline _renderer = null!;
    private GameMessagePipeline _pipeline  = null!;
    
    private bool _isDisposed = false;

    private const float FixedDt = 1f / 50f; // 50 Hz simulation, adjust as needed
    private int _simulationTick = 0;
    private float _accumulatorForTick = 0;
    
    private float _accumulatorFpsCounter = 0;
    
    internal GameEngine(
        GameProgram program,
        WindowOptions windowOptions,
        GraphicsBackend backend,
        AssetManagerConfiguration assetPipelineConfiguration
    )
    {
        _boundProgram = program;
        
        _window = Window.Create(windowOptions);

        _window.Load += () => Load(backend, assetPipelineConfiguration);
        _window.Update += Update;
        _window.Render += Render;
        _window.Closing += Close;
    }

    public void Run()
    {
        _window.Run();
        _window?.Dispose();
    }

    private void Load(GraphicsBackend backend, AssetManagerConfiguration assetPipelineConfiguration)
    {
        var initialFrameContext = new RenderFrameContext
        {
            DeltaTime = 0,
            FramebufferSize = _window.FramebufferSize,
            ViewportSize = _window.Size
        };

        _input = new InputManager(_window.CreateInput());

        // graphics
        _graphics = backend switch
        {
            GraphicsBackend.OpenGL => new GlGraphicsDevice(_window.CreateOpenGL(), in initialFrameContext),
            _ => throw new GraphicsException("Invalid GraphicsBackend. Only OpenGL supported")
        };
        
        // assets
        _assets = new AssetManager(graphics: _graphics, assetPath: assetPipelineConfiguration.AssetPath,
            manifestFilename: assetPipelineConfiguration.ManifestFilename);
        _assets.LoadFromManifest();
        
        // messages
        _pipeline = new GameMessagePipeline();

        // renderer
        var shaders = _assets.GetAll<Shader>();
        _renderer = new RenderPipeline(_graphics, shaders.ToArray());
        
        var context = new GameEngineContext(
            pipeline: _pipeline,
            input: _input,
            assets: _assets,
            graphics: _graphics,
            renderer: _renderer 
        );
        
        _boundProgram.BindGameProgram(context);
    }

    private void Update(double delta)
    {
        float dt = (float)delta;
        _input.Update();

        // fixed-step simulation
        _accumulatorForTick += dt;
        while (_accumulatorForTick >= FixedDt)
        {
            _pipeline.ProcessTick(_simulationTick);
            _simulationTick++;
            _accumulatorForTick -= FixedDt;
        }
        
        _boundProgram.UpdateInternal(dt);
    }

    private void Render(double delta)
    {
        float dt = (float)delta;
        
        float fps = dt > 0 ? 1.0f / dt : 0.0f;

        /*
        _accumulatorFpsCounter += dt;
        if (_accumulatorFpsCounter >= 0.5f)
        {
            Console.WriteLine($"Fps: {fps}");
            _accumulatorFpsCounter = 0;
        }
*/
        
        var frameCtx = new RenderFrameContext
        {
            DeltaTime = dt,
            FramesPerSecond = fps,
            FramebufferSize = _window.FramebufferSize,
            ViewportSize = _window.Size
        };

        _graphics.StartFrame(in frameCtx);
        _renderer.Prepare();
        _boundProgram.RenderInternal(dt);
        _graphics.StartDraw();
        _renderer.Execute();
        _graphics.EndFrame();
        

    }

    private void Close()
    {
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;
        
        _assets?.Dispose();
        _graphics?.Dispose();
    }

    
    public void Dispose()
    {
        if(_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;

        _assets?.Dispose();
        _graphics?.Dispose();
    }
    
}