using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

internal class RenderTargetRegistry
{
    private readonly IGraphicsDevice _graphics;

    // Scene Fbo
    private RenderPassEntry _multisampleFbo;
    private RenderPassEntry _sceneFbo;

    // Effect Fbo
    private RenderPassEntry _lightFbo;

    private readonly List<IRenderPass>[] _renderTargets;

    public RenderTargetEnumerator GetEnumerator() => new(_renderTargets);

    public RenderTargetRegistry(IGraphicsDevice graphics)
    {
        _graphics = graphics;

        _renderTargets = new List<IRenderPass>[RenderConsts.RenderTargetCount];
        for (int i = 0; i < RenderConsts.RenderTargetCount; i++)
        {
            _renderTargets[i] = new List<IRenderPass>(4);
        }
    }

    public void RegisterRenderTargetsFrom(RenderTargetDescription desc)
    {
        ArgumentNullException.ThrowIfNull(desc);
        ArgumentNullException.ThrowIfNull(desc.SceneTarget);
        ArgumentNullException.ThrowIfNull(desc.LightTarget);
        ArgumentNullException.ThrowIfNull(desc.ScreenTarget);
        desc.LightTarget.LightShader.IsValidOrThrow();
        desc.ScreenTarget.CompositeShaderId.IsValidOrThrow();

        // Scene Target setup
        var sceneTarget = desc.SceneTarget;
        CreateSceneBuffer();
        CreateMultisampleBuffer(Vector2.One, sceneTarget.Samples);

        // Light Target setup
        var lightTarget = desc.LightTarget;
        CreateLightBuffer(lightTarget.SizeRatio, lightTarget.TexPreset);

        // Screen Target setup
        var screenTarget = desc.ScreenTarget;

        // Scene Passes
        // Pass 0: draw scene into MSAA FBO
        RegisterRenderPass(RenderTargetId.Scene, new SceneRenderPass
        {
            TargetFbo = _multisampleFbo.FboId,
            Clear = new RenderPassClearDesc(Colors.CornflowerBlue, ClearBufferFlag.ColorAndDepth)
        });

        // Pass 1: resolve MSAA into single-sample texture FBO
        RegisterRenderPass(RenderTargetId.Scene, new BlitRenderPass
        {
            TargetFbo = _sceneFbo.FboId,
            BlitFbo = _multisampleFbo.FboId,
            Multisample = true,
            Samples = desc.SceneTarget.Samples
        });

        // Light Passes
        // Pass 0: Draw light into FBO
        RegisterRenderPass(RenderTargetId.SceneLight, new LightRenderPass
        {
            TargetFbo = _lightFbo.FboId,
            Shader = lightTarget.LightShader,
            Clear = new RenderPassClearDesc(lightTarget.ClearColor, ClearBufferFlag.Color),
            Blend = lightTarget.Blend,
            DepthTest = false
        });
        
        // Screen Passes
        // Pass 0: Combine scene and light fbo texture into final scene
        RegisterRenderPass(RenderTargetId.Screen, new FsqRenderPass
        {
            TargetFbo = default,
            SourceTextures = [_sceneFbo.FboMeta.ColTexId, _lightFbo.FboMeta.ColTexId],
            Shader = screenTarget.CompositeShaderId
        });
    }

    private void RegisterRenderPass(RenderTargetId target, IRenderPass pass)
    {
        if (pass.Op == RenderPassOp.FullscreenQuad && pass is not FsqRenderPass)
            throw new InvalidOperationException($"RenderPass: FullscreenQuad require {nameof(FsqRenderPass)}");

        if (pass.Op == RenderPassOp.Blit && pass is not BlitRenderPass)
            throw new InvalidOperationException($"RenderPass: Blit require {nameof(BlitRenderPass)}");


        _renderTargets[(int)target].Add(pass);
    }
    

    private void CreateSceneBuffer()
    {
        if (_sceneFbo.IsValid())
            throw new InvalidOperationException("Scene buffer is already created");

        var fboId =
            _graphics.CreateFramebuffer(new FrameBufferDesc(SizeRatio: Vector2.One, DepthStencilBuffer: true),
                out var meta);

        _sceneFbo = new RenderPassEntry(fboId, meta);
    }

    private void CreateMultisampleBuffer(Vector2 sizeRatio, uint samples)
    {
        ValidateSizeRatio(sizeRatio);
        if (samples is not (0 or 2 or 4 or 8))
            throw new ArgumentOutOfRangeException(nameof(samples), "Valid samples are 0,2,4,8");

        if (_multisampleFbo.IsValid())
            throw new InvalidOperationException("Multisample buffer is already created");

        var fboId = _graphics.CreateFramebuffer(new FrameBufferDesc(
            SizeRatio: sizeRatio, DepthStencilBuffer: true, Msaa: samples > 0, Samples: samples), out var meta);

        _multisampleFbo = new RenderPassEntry(fboId, meta);
    }

    private void CreateLightBuffer(Vector2 sizeRatio, TexturePreset preset)
    {
        ValidateSizeRatio(sizeRatio);
        if (_lightFbo.IsValid())
            throw new InvalidOperationException("Light buffer is already created");

        var fboId = _graphics.CreateFramebuffer(
            new FrameBufferDesc(SizeRatio: sizeRatio, TexturePreset: preset,
                DepthStencilBuffer: false), out var meta);

        _lightFbo = new RenderPassEntry(fboId, meta);
    }

    private void ValidateSizeRatio(Vector2 sizeRatio)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sizeRatio.X, 0, nameof(sizeRatio.X));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sizeRatio.Y, 0, nameof(sizeRatio.Y));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sizeRatio.X, 1, nameof(sizeRatio.X));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sizeRatio.Y, 1, nameof(sizeRatio.Y));
    }

    public struct RenderTargetEnumerator(List<IRenderPass>[] renderTargets)
    {
        private int _i = -1;
        public bool MoveNext() => ++_i < renderTargets.Length;
        public (RenderTargetId targetId, IReadOnlyList<IRenderPass>) Current => ((RenderTargetId)_i, renderTargets[_i]);
        public RenderTargetEnumerator GetEnumerator => this;
    }

    private struct RenderPassEntry(FrameBufferId fboId, FrameBufferMeta fboMeta)
    {
        public readonly FrameBufferId FboId = fboId;
        public readonly FrameBufferMeta FboMeta = fboMeta;
        public ShaderId ShaderId = default;

        public bool IsValid() => FboId.IsValid();
    }
}