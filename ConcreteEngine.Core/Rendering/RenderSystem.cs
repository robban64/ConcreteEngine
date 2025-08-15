#region

using System.Drawing;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Core.Rendering.Sprite;
using ConcreteEngine.Core.Rendering.Tilemap;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using Silk.NET.Maths;
using static ConcreteEngine.Core.Rendering.RenderConsts;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderSystem : IGameEngineSystem
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _ctx;
    private readonly ViewTransform2D _camera;
    
    private readonly Shader[] _shaders;
    private readonly MaterialStore _materialStore;
    private readonly SortedList<int, DrawCommandId>[] _renderPasses;
    private readonly SortedList<int, RenderPassDesc>[] _renderPassDesc;

    private readonly DrawCommandCollector _commandCollector;
    private readonly DrawCommandSubmitter _commandSubmitter;
    private readonly DrawEmitterContext _emitterContext;

    private readonly SpriteBatcher _spriteBatch;
    private readonly TilemapBatcher _tilemapBatcher;

    private Shader _screenShader;


    public SpriteBatcher SpriteBatch => _spriteBatch;

    internal RenderSystem(IGraphicsDevice graphics, ViewTransform2D camera, Shader[] shaders)
    {
        var a = new RenderPassDesc();
        _graphics = graphics;
        _ctx = graphics.Ctx;
        _camera = camera;
        
        _shaders =  shaders.ToArray();
        _materialStore = new MaterialStore();

        _renderPasses = new SortedList<int, DrawCommandId>[RenderTargetCount];
        _renderPassDesc = new SortedList<int, RenderPassDesc>[RenderTargetCount];
        for (int i = 0; i < RenderTargetCount; i++)
        {
            _renderPasses[i] = new SortedList<int, DrawCommandId>(4);
            _renderPassDesc[i] = new  SortedList<int, RenderPassDesc>(4);
        }

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
        
        _screenShader = shaders.First(x => x.Name == "ScreenShader");

    }

    public void RegisterRenderPass(in CreateRenderPassDesc desc)
    {
        var fboCtx = _graphics.CreateFrameBuffer(in desc);
        _renderPassDesc[(int)desc.Target].Add(desc.Order, fboCtx);
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


    internal void Render(float alpha, in GraphicsFrameContext frameCtx)
    {
        _ctx.BeginFrame(in frameCtx);
        PrepareRenderer();
        
        //_ctx.BeginRenderPass(_framebufferId, Color.CornflowerBlue, ClearBufferFlag.ColorAndDepth);
        Execute(alpha);
        //_ctx.EndRenderPass();
        
        //_ctx.ResolveFramebufferTo(_framebufferId);
        //_ctx.DrawFboScreenQuad(_framebufferId, _screenShader.ResourceId);
        
        _ctx.EndFrame();
        _graphics.CleanupAfterRender();

        /*
        _ctx.SetBlendMode(BlendMode.None);
        _ctx.UseShader(_screenShader.ResourceId);
        _ctx.BindFramebufferTexture(_framebufferId);
        _ctx.BindMesh(_graphics.Ctx.QuadMeshId);
        _ctx.Draw();
        */
        
    }
    
    private void PrepareRenderer()
    {
        _commandSubmitter.ResetBufferPointer();
        _commandCollector.Collect(_emitterContext, _commandSubmitter);
    }

    private void Execute(float alpha)
    {
        var projectionViewMatrix = _camera.ProjectionViewMatrix;
        
        // setup the projection view matrix for all shaders
        foreach (var shader in _shaders)
        {
            _ctx.UseShader(shader.ResourceId);
            _ctx.SetUniform(ShaderUniform.ProjectionViewMatrix, in  projectionViewMatrix);
        }
        
        //_ctx.BeginRenderPass(_framebufferId, Color.CornflowerBlue, ClearBufferFlag.ColorAndDepth);
        //_ctx.EndRenderPass();
        //_ctx.ResolveFramebufferTo(_framebufferId);
        //_ctx.DrawFboScreenQuad(_framebufferId, _screenShader.ResourceId);
        for (int target = 0; target < RenderTargetCount; target++)
        {
            var renderTarget = (RenderTargetId)target;
            var passList = _renderPassDesc[target];
            for (int p = 0; p < passList.Count; p++)
            {
                var pass = passList[p];
                if (pass.FboId == 0)
                    _ctx.BeginScreenPass(pass.Clear ? pass.ClearColor : null, pass.ClearMask);
                else
                    _ctx.BeginRenderPass(pass.FboId,  pass.Clear ? pass.ClearColor : null, pass.ClearMask);
                
                foreach (var (_, commandId) in _renderPasses[target])
                {
                    var commands = _commandSubmitter.GetQueue(renderTarget, commandId);

                    for (int i = 0; i < commands.Length; i++)
                    {
                        ref readonly var msg = ref commands[i];
                        Draw(in msg.Cmd, in msg.Info);
                    }
                }
                
                if(pass.FboId != 0) _ctx.EndRenderPass();

                switch (pass.ResolveTo)
                {
                    case RenderPassResolveTarget.Blit:
                        _ctx.BlitFramebufferTo(pass.FboId, pass.ResolveToFboId);
                        break;
                    case RenderPassResolveTarget.FullscreenQuad:
                        _ctx.DrawFboScreenQuad(pass.FboId, _screenShader.ResourceId);
                        break;
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

    public void Dispose()
    {
        
    }
}