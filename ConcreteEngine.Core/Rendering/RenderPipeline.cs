#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Rendering.SpriteBatching;
using ConcreteEngine.Core.Rendering.Tilemap;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderPipeline
{
    private static int RenderTargetCount = Enum.GetValues<RenderTargetId>().Length;
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _ctx;

    private readonly DrawCommandCollector _commandCollector;
    private readonly DrawCommandSubmitter _commandSubmitter;
    private readonly DrawEmitterContext _emitterContext;

    private readonly SpriteBatcher _spriteBatch;
    private readonly TilemapBatcher _tilemapBatcher;

    private readonly SortedList<int, DrawCommandId>[] _renderPasses;

    public SpriteBatcher SpriteBatch => _spriteBatch;


    internal RenderPipeline(IGraphicsDevice graphics)
    {
        _graphics = graphics;
        _ctx = graphics.Ctx;

        _renderPasses = new SortedList<int, DrawCommandId>[RenderTargetCount];
        for (int i = 0; i < RenderTargetCount; i++)
            _renderPasses[i] = new SortedList<int, DrawCommandId>(4);

        _commandCollector = new DrawCommandCollector();
        _commandSubmitter = new DrawCommandSubmitter();

        _spriteBatch = new SpriteBatcher(graphics, this);
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


    internal void Prepare()
    {
    }

    internal void Execute()
    {
        _commandSubmitter.ResetBufferPointer();

        _commandCollector.Collect(_emitterContext, _commandSubmitter);

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
        _ctx.UseShader(data.ShaderId);
        _ctx.SetUniform(ShaderUniform.ProjectionViewMatrix, _ctx.ViewTransform.ProjectionViewMatrix);

        _ctx.SetUniform(ShaderUniform.ModelMatrix, in data.Transform);
        _ctx.SetUniform(ShaderUniform.SampleTexture, 0);

        _ctx.BindTexture(data.TextureId, 0);

        _ctx.BindMesh(data.MeshId);
        _ctx.DrawIndexed(data.DrawCount);
    }
}