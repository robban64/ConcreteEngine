#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Input;
using ConcreteEngine.Core.Pipeline;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core;

public sealed class GameEngineContext
{
    private readonly GameMessagePipeline _pipeline;
    public InputManager Input { get; init; }
    public AssetManager Assets { get; init; }
    public IGraphicsDevice Graphics { get; init; }
    
    public RenderPipeline Renderer { get; init; }

    internal GameEngineContext(
        GameMessagePipeline pipeline, 
        InputManager input, 
        AssetManager assets,
        IGraphicsDevice graphics,  
        RenderPipeline renderer
        )
    {
        _pipeline = pipeline;
        Input = input;
        Assets = assets;
        Graphics = graphics;
        Renderer = renderer;
    }

    public IDisposable Subscribe<TEvent>(Action<IGameEvent> handler) where TEvent : IGameEvent =>
        _pipeline.Subscribe<TEvent>(handler);

    public void Publish<TCommand>(TCommand command) where TCommand : IGameCommand => _pipeline.Enqueue(command);
}