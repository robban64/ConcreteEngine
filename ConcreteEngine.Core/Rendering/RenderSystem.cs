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
using ConcreteEngine.Graphics.OpenGL;
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
    private readonly SortedList<int, RenderPassData>[] _renderPassDesc;

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

        _shaders = shaders.ToArray();
        _materialStore = new MaterialStore();

        _renderPasses = new SortedList<int, DrawCommandId>[RenderTargetCount];
        _renderPassDesc = new SortedList<int, RenderPassData>[RenderTargetCount];
        for (int i = 0; i < RenderTargetCount; i++)
        {
            _renderPasses[i] = new SortedList<int, DrawCommandId>(4);
            _renderPassDesc[i] = new SortedList<int, RenderPassData>(4);
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

    public void RegisterRenderPass(RenderTargetId target, int order, in RenderPassData param)
    {
        //var (fboId, texId) = _graphics.CreateFramebuffer(param.SizeRatio);

        var r = param;
        /*
        if (param.Op == RenderPassOp.FullscreenQuad)
        {
            if(param.ReadTexId == null)
             r = r with { ReadTexId = texId };
            if(param.WriteFboId == 0)
             r = r with { WriteFboId = fboId };
        }
        */
        _renderPassDesc[(int)target].Add(order, r);
    }

    /*
    public void RegisterRenderPass(string name, Shader? shader, in RegisterRenderTargetDesc param)
    {
        var key = _graphics.CreateRenderTarget(param.Target, param.SizeRatio);
        var renderPass = RenderPass.From(key, shader, in param);
        _renderPassDesc[(int)param.Target].Add(param.Order, renderPass);
    }
    */
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
            _gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, in projectionViewMatrix);
        }

        for (int target = 0; target < RenderTargetCount; target++)
        {
            var renderTarget = (RenderTargetId)target;
            var passList = _renderPassDesc[target];
            for (int p = 0; p < passList.Count; p++)
            {
                ExecutePass(renderTarget, passList[p], p == passList.Count - 1);
            }
        }
    }

    private void ExecutePass(RenderTargetId target, RenderPassData pass, bool lastPass)
    {
        if (pass.WriteFboId == 0)
            _gfx.BeginScreenPass(pass.DoClear ? pass.ClearColor : null, pass.ClearMask);
        else
            _gfx.BeginRenderPass(pass.WriteFboId, pass.DoClear ? pass.ClearColor : null, pass.ClearMask);

        if (pass.Op == RenderPassOp.DrawScene)
        {
            foreach (var (_, commandId) in _renderPasses[(int)target])
            {
                var commands = _commandSubmitter.GetQueue(target, commandId);

                for (int i = 0; i < commands.Length; i++)
                {
                    ref readonly var msg = ref commands[i];
                    Draw(in msg.Cmd, in msg.Info);
                }
            }
        }


        switch (pass.Op)
        {
            case RenderPassOp.Blit:
                _gfx.BlitFramebufferTo(pass.WriteFboId, pass.BlitFboId.Value);
                _gfx.EndRenderPass();
                return;
                break;
            case RenderPassOp.FullscreenQuad:
                if (pass.WriteFboId == 0)_gfx.EndRenderPass();
                var colTex = pass.ReadTexId!.Value;
                DrawFboScreenQuad(colTex, pass.ShaderId);
                if(pass.WriteFboId == 0) return;
                break;
        }
        
        if (pass.WriteFboId != 0) _gfx.EndRenderPass();

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

    private void DrawFboScreenQuad( ushort colTexId, ushort shaderId)
    {
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