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
        if (param.Op == RenderPassOp.FullscreenQuad && param.SourceTexId == null)
        {
            throw new InvalidOperationException(
                $"FullscreenQuad requires {nameof(param.SourceTexId)} (source texture).");
        }

        if (param.Op == RenderPassOp.Blit && param.SourceTexId != null)
        {
            throw new InvalidOperationException(
                $"Blit op requires {nameof(param.BlitFboId)} (source framebuffer).");
        }

        _renderPassDesc[(int)target].Add(order, param);
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
    }

    private void PrepareRenderer()
    {
        _commandSubmitter.ResetBufferPointer();
        _commandCollector.Collect(_emitterContext, _commandSubmitter);
    }

    private void Execute(float alpha)
    {
        for (int target = 0; target < 1; target++)
        {
            var renderTarget = (RenderTargetId)target;
            var passList = _renderPassDesc[target];
            for (int p = 0; p < passList.Count; p++)
            {
                var pass = passList[p];
                var (prevBlend, prevDepthTest) = (_gfx.BlendMode, _gfx.DepthTest);
                _gfx.SetBlendMode(pass.Blend);
                _gfx.SetDepthTest(pass.DepthTest);

                ExecutePass(renderTarget, in pass);

                _gfx.SetBlendMode(prevBlend);
                _gfx.SetDepthTest(prevDepthTest);
            }
        }
    }

    private void ExecutePass(RenderTargetId target, in RenderPassData pass)
    {
        if (pass.Op == RenderPassOp.Blit)
        {
            // preserves bindings internally
            _gfx.BlitFramebuffer(pass.BlitFboId!.Value, pass.TargetFboId, linearFilter: true); 
            return;
        }

        var isScreenPass = pass.TargetFboId == 0;

        _gfx.SetBlendMode(pass.Blend);
        _gfx.SetDepthTest(pass.DepthTest);

        if (pass.TargetFboId == 0)
            _gfx.BeginScreenPass(pass.DoClear ? pass.ClearColor : null, pass.ClearMask);
        else
            _gfx.BeginRenderPass(pass.TargetFboId, pass.DoClear ? pass.ClearColor : null, pass.ClearMask);


        if (pass.Op == RenderPassOp.DrawScene)
        {
            _gfx.SetBlendMode(pass.Blend);
            _gfx.SetDepthTest(pass.DepthTest);
            ExecuteDrawScenePass(target);
            _gfx.EndRenderPass();
        }
        else if (pass.Op == RenderPassOp.FullscreenQuad)
        {
            var colTex = pass.SourceTexId!.Value;
            DrawFboScreenQuad(colTex, pass.ShaderId);
        }

        if (!isScreenPass)
        {
            _gfx.EndRenderPass();
        }
    }

    private void ExecuteDrawScenePass(RenderTargetId target)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Draw(in DrawCommandData data, in DrawCommandMeta meta)
    {
        var projectionViewMatrix = _camera.ProjectionViewMatrix;

        var material = _materialStore[data.MaterialId];
        material.Bind(_gfx);
        _gfx.UseShader(material.Shader.ResourceId);
        _gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, in projectionViewMatrix);

        _gfx.SetUniform(ShaderUniform.ModelMatrix, in data.Transform);
        _gfx.BindMesh(data.MeshId);
        _gfx.DrawIndexed(data.DrawCount);
    }

    private void DrawFboScreenQuad(ushort colTexId, ushort shaderId)
    {
        _gfx.UseShader(shaderId);
        _gfx.BindTexture(colTexId, 0);
        _gfx.BindMesh(_graphics.QuadMeshId);
        _gfx.Draw();
    }

    public void Dispose()
    {
    }
}