#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Core.Rendering.SpriteBatching;
using ConcreteEngine.Core.Rendering.Tilemap;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using Silk.NET.Maths;
using static ConcreteEngine.Core.Rendering.RenderConsts;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderPipeline
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _ctx;
    
    private readonly Shader[] _shaders;

    private readonly DrawCommandCollector _commandCollector;
    private readonly DrawCommandSubmitter _commandSubmitter;
    private readonly DrawEmitterContext _emitterContext;

    private readonly SpriteBatcher _spriteBatch;
    private readonly TilemapBatcher _tilemapBatcher;

    private readonly SortedList<int, DrawCommandId>[] _renderPasses;

    private readonly MaterialStore _materialStore;

    public SpriteBatcher SpriteBatch => _spriteBatch;
    


    internal RenderPipeline(IGraphicsDevice graphics, Shader[] shaders)
    {
        _graphics = graphics;
        _ctx = graphics.Ctx;
        
        _shaders =  shaders.ToArray();
        _materialStore = new MaterialStore();

        _renderPasses = new SortedList<int, DrawCommandId>[RenderTargetCount];
        for (int i = 0; i < RenderTargetCount; i++)
            _renderPasses[i] = new SortedList<int, DrawCommandId>(4);

        _commandCollector = new DrawCommandCollector();
        _commandSubmitter = new DrawCommandSubmitter();

        _spriteBatch = new SpriteBatcher(graphics);
        _tilemapBatcher = new TilemapBatcher(graphics, 64, 32);

        _emitterContext = new DrawEmitterContext
        {
            Graphics = _graphics,
            SpriteBatch = _spriteBatch,
            TilemapBatch = _tilemapBatcher
        };
    }

    public void RegisterCommand(int order, DrawCommandId commandId, RenderTargetId target, int capacity)
    {
        _commandSubmitter.RegisterCommand(commandId, target, capacity);
        _renderPasses[(int)target].Add(order, commandId);
    }

    public void RegisterEmitter<T>(int order, T emitter) where T : class, IDrawCommandEmitter
        => _commandCollector.RegisterEmitter<T>(order, emitter);

    public void AddMaterial(MaterialDescription description)
        => _materialStore.AddMaterial(description);
    
    internal void Prepare()
    {
        _commandSubmitter.ResetBufferPointer();
        _commandCollector.Collect(_emitterContext, _commandSubmitter);

    }

    internal void Execute()
    {
        var projectionViewMatrix = _ctx.ViewTransform.ProjectionViewMatrix;
        
        // setup the projection view matrix for all shaders
        foreach (var shader in _shaders)
        {
            _ctx.UseShader(shader.ResourceId);
            _ctx.SetUniform(ShaderUniform.ProjectionViewMatrix, in  projectionViewMatrix);
        }
        
        
        for (int target = 0; target < RenderTargetCount; target++)
        {
            var renderTarget = (RenderTargetId)target;
            foreach (var (_, commandId) in _renderPasses[target])
            {
                var commands = _commandSubmitter.GetQueue(renderTarget, commandId);

                for (int i = 0; i < commands.Length; i++)
                {
                    ref readonly var msg = ref commands[i];
                    Draw(in msg.Cmd, in msg.Info);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Draw(in DrawCommandData data, in DrawCommandMeta meta)
    {
        var material = _materialStore[data.MaterialId];
        material.Bind(_ctx);
        _ctx.SetUniform(ShaderUniform.ModelMatrix, in data.Transform);
        _ctx.BindMesh(data.MeshId);
        _ctx.DrawIndexed(data.DrawCount);
    }
}