#region

using System.Drawing;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Core.Rendering.Sprite;
using ConcreteEngine.Core.Rendering.Tilemap;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;
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
    private readonly IGraphicsContext _gfx;
    private readonly ViewTransform2D _camera;
    
    private readonly Shader[] _shaders;
    private readonly MaterialStore _materialStore;
    private readonly SortedList<int, DrawCommandId>[] _renderPasses;
    private readonly SortedList<int, RenderPass>[] _renderPassDesc;

    private readonly Dictionary<string, RenderPass> _renderPassDict;

    private readonly DrawCommandCollector _commandCollector;
    private readonly DrawCommandSubmitter _commandSubmitter;
    private readonly DrawEmitterContext _emitterContext;

    private readonly SpriteBatcher _spriteBatch;
    private readonly TilemapBatcher _tilemapBatcher;

    public SpriteBatcher SpriteBatch => _spriteBatch;

    internal RenderSystem(IGraphicsDevice graphics, ViewTransform2D camera, Shader[] shaders)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        _camera = camera;
        
        _shaders =  shaders.ToArray();
        _materialStore = new MaterialStore();

        _renderPassDict = new Dictionary<string, RenderPass>(4);
        _renderPasses = new SortedList<int, DrawCommandId>[RenderTargetCount];
        _renderPassDesc = new SortedList<int, RenderPass>[RenderTargetCount];
        for (int i = 0; i < RenderTargetCount; i++)
        {
            _renderPasses[i] = new SortedList<int, DrawCommandId>(4);
            _renderPassDesc[i] = new  SortedList<int, RenderPass>(4);
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
        
    }

    public void RegisterRenderPass(string name, Shader? shader, in RegisterRenderTargetDesc param)
    {
        var key = _graphics.CreateRenderTarget(param.Target, param.SizeRatio);
        var renderPass = RenderPass.From(key, shader, in param);
        _renderPassDesc[(int)param.Target].Add(param.Order, renderPass);
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
        _graphics.StartFrame(in frameCtx);
        PrepareRenderer();
        Execute(alpha);
        _graphics.EndFrame();

        
        //_gfx.BeginRenderPass(_framebufferId, Color.CornflowerBlue, ClearBufferFlag.ColorAndDepth);
        //_gfx.EndRenderPass();
        
        //_gfx.ResolveFramebufferTo(_framebufferId);
        //_gfx.DrawFboScreenQuad(_framebufferId, _screenShader.ResourceId);
        

        /*
        _gfx.SetBlendMode(BlendMode.None);
        _gfx.UseShader(_screenShader.ResourceId);
        _gfx.BindFramebufferTexture(_framebufferId);
        _gfx.BindMesh(_graphics.Ctx.QuadMeshId);
        _gfx.Draw();
        */
        
    }
    
    /*
     public void DrawFboScreenQuad(ushort fboId, ushort shaderId)
       {
           ArgumentOutOfRangeException.ThrowIfZero(fboId, nameof(fboId));
           ArgumentOutOfRangeException.ThrowIfZero(fboId, nameof(shaderId));

           var fbo = _store.Get<GlFramebuffer>(fboId);
           ValidateResource(fbo);
           if (fbo.ColorTextureId == 0) GraphicsException.ThrowInvalidState("FBO has no color texture.");

           var previousBlendMode = _blendMode;

           SetBlendMode(BlendMode.None);
           UseShader(shaderId);
           BindTexture(fbo.ColorTextureId, 0);
           BindMesh(QuadMeshId);
           Draw();

           SetBlendMode(previousBlendMode);
       }
     */
    
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
            _gfx.UseShader(shader.ResourceId);
            _gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, in  projectionViewMatrix);
        }
        
        for (int target = 0; target < RenderTargetCount; target++)
        {
            var renderTarget = (RenderTargetId)target;
            var passList = _renderPassDesc[target];
            for (int p = 0; p < passList.Count; p++)
            {
                ExecutePass(passList[p]);
            }
            
        }
    }

    private void ExecutePass(RenderPass pass)
    {
        var target =  pass.Target;
        var (fboId, colTexId) = _graphics.GetRenderTarget(pass.GfxKey);
        
        if (fboId == 0)
            _gfx.BeginScreenPass(pass.DoClear ? pass.ClearColor : null, pass.ClearMask);
        else
            _gfx.BeginRenderPass(fboId,  pass.DoClear ? pass.ClearColor : null, pass.ClearMask);
                
        foreach (var (_, commandId) in _renderPasses[(int)target])
        {
            var commands = _commandSubmitter.GetQueue(pass.Target, commandId);

            for (int i = 0; i < commands.Length; i++)
            {
                ref readonly var msg = ref commands[i];
                Draw(in msg.Cmd, in msg.Info);
            }
        }
                
        if(fboId != 0) _gfx.EndRenderPass();

        switch (pass.ResolveTo)
        {
            case RenderPassResolveTarget.Blit:
                _gfx.BlitFramebufferTo(fboId, pass.ResolveToTarget.Key);
                break;
            case RenderPassResolveTarget.FullscreenQuad:
                DrawFboScreenQuad(fboId, colTexId, pass.ShaderId);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Draw(in DrawCommandData data, in DrawCommandMeta meta)
    {
        var material = _materialStore[data.MaterialId];
        material.Bind(_gfx);
        _gfx.SetUniform(ShaderUniform.ModelMatrix, in data.Transform);
        _gfx.BindMesh(data.MeshId);
        _gfx.DrawIndexed(data.DrawCount);
    }

    private void DrawFboScreenQuad(ushort fboId, ushort colTexId, ushort shaderId)
    {
        ArgumentOutOfRangeException.ThrowIfZero(fboId, nameof(fboId));
        ArgumentOutOfRangeException.ThrowIfZero(fboId, nameof(shaderId));
        ArgumentOutOfRangeException.ThrowIfZero(fboId, nameof(colTexId));

        var previousBlendMode = _gfx.BlendMode;
        _gfx.SetBlendMode(BlendMode.None);
        _gfx.UseShader(shaderId);
        _gfx.BindTexture(colTexId, 0);
        _gfx.BindMesh(_graphics.QuadMeshId);
        _gfx.Draw();
        _gfx.SetBlendMode(previousBlendMode);
    }
    
    public void Dispose()
    {
        
    }
}