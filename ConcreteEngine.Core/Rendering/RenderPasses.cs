using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public struct RenderPassMutation
{
    public Color4? ClearColor;
}

internal class RenderPasses
{
    private readonly IGraphicsDevice _graphics;

    // Scene Fbo
    private RenderPassEntry _multisampleFbo;
    private RenderPassEntry _sceneFbo;

    // Effect Fbo
    private RenderPassEntry _lightFbo;
    
    public RenderPassEntry MultisampleFbo => _multisampleFbo;
    public RenderPassEntry SceneFbo => _sceneFbo;
    public RenderPassEntry LightFbo => _lightFbo;

    private readonly List<IRenderPassDescriptor>[] _renderTargets;

    public RenderTargetEnumerator GetEnumerator() => new(_renderTargets);

    public RenderPasses(IGraphicsDevice graphics)
    {
        _graphics = graphics;
        
        _renderTargets = new List<IRenderPassDescriptor>[RenderConsts.RenderTargetCount];
        for (int i = 0; i < RenderConsts.RenderTargetCount; i++)
        {
            _renderTargets[i] = new List<IRenderPassDescriptor>(4);
        }
    }

    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation)
    {
        switch (targetId)
        {
            case RenderTargetId.Scene:
                var scenePass = (SceneRenderPass)(_renderTargets[(int)RenderTargetId.Scene][0]);
                var sceneClear = scenePass.Clear!.Value;
                scenePass.Clear = sceneClear with { ClearColor = mutation.ClearColor!.Value };
                break;
            case RenderTargetId.SceneLight:
                var lightPass = (LightRenderPass)(_renderTargets[(int)RenderTargetId.SceneLight][0]);
                var lightClear = lightPass.Clear!.Value;
                lightPass.Clear = lightClear with { ClearColor = mutation.ClearColor!.Value };
                break;
            case RenderTargetId.Screen:
                break;
        }
    }

    public void RegisterRenderPass(RenderTargetId target, IRenderPassDescriptor pass)
    {
        if (pass.Op == RenderPassOp.FullscreenQuad && pass is not FsqRenderPass)
            throw new InvalidOperationException($"RenderPass: FullscreenQuad require {nameof(FsqRenderPass)}");

        if (pass.Op == RenderPassOp.Blit && pass is not BlitRenderPass)
            throw new InvalidOperationException($"RenderPass: Blit require {nameof(BlitRenderPass)}");


        _renderTargets[(int)target].Add(pass);
    }
    

    public void CreateSceneBuffer()
    {
        if (_sceneFbo.IsValid())
            throw new InvalidOperationException("Scene buffer is already created");

        var fboId =
            _graphics.CreateFramebuffer(new FrameBufferDesc(SizeRatio: Vector2.One, DepthStencilBuffer: true),
                out var meta);

        _sceneFbo = new RenderPassEntry(fboId, meta);
    }

    public void CreateMultisampleBuffer(Vector2 sizeRatio, uint samples)
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

    public void CreateLightBuffer(Vector2 sizeRatio, TexturePreset preset)
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

    public struct RenderPassEntry(FrameBufferId fboId, FrameBufferMeta fboMeta)
    {
        public readonly FrameBufferId FboId = fboId;
        public readonly FrameBufferMeta FboMeta = fboMeta;
        public ShaderId ShaderId = default;

        public bool IsValid() => FboId.IsValid();
    }
}

public struct RenderTargetEnumerator(List<IRenderPassDescriptor>[] renderTargets)
{
    private int _i = -1;
    public bool MoveNext() => ++_i < renderTargets.Length;
    public (RenderTargetId targetId, IReadOnlyList<IRenderPassDescriptor>) Current => ((RenderTargetId)_i, renderTargets[_i]);
    public RenderTargetEnumerator GetEnumerator => this;
}