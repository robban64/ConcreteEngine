using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

public struct RenderPassMutation
{
    public Color4? ClearColor;
}

internal class RenderPasses
{
    private readonly IGraphicsRuntime _graphics;
    private readonly RenderGlobalSnapshot _snapshot;

    private IGfxFactoryHub GfxFactory => _graphics.FactoryHub;
    private IGfxResourceAllocator GfxAllocator => _graphics.Allocator;

    public RenderPassFboRecord MultisampleFbo { get; private set; }
    public RenderPassFboRecord SceneFbo { get; private set; }
    public RenderPassFboRecord LightFbo { get; private set; }
    public RenderPassFboRecord ShadowFbo { get; private set; }
    public RenderPassFboRecord PostFboA { get; private set; }
    public RenderPassFboRecord PostFboB { get; private set; }

    private readonly List<IRenderPassDescriptor>[] _renderTargets;

    private int _currentTargetId = 0;
    

    public RenderPasses(IGraphicsRuntime graphics, in RenderGlobalSnapshot snapshot)
    {
        _graphics = graphics;
        _snapshot = snapshot;

        _renderTargets = new List<IRenderPassDescriptor>[RenderConsts.RenderTargetCount];
        for (int i = 0; i < RenderConsts.RenderTargetCount; i++)
        {
            _renderTargets[i] = new List<IRenderPassDescriptor>(4);
        }
    }

    public bool TryGetNextPasses(out RenderTargetId targetId, out List<IRenderPassDescriptor> passes)
    {
        if (_currentTargetId >=  RenderConsts.RenderTargetCount)
        {
            _currentTargetId = 0;
            targetId = (RenderTargetId)_currentTargetId;
            passes = _renderTargets[(int)targetId]; 
            return false;
        }
        targetId = (RenderTargetId)_currentTargetId;
        passes = _renderTargets[_currentTargetId];
        _currentTargetId++;
        return true;
    }

    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation)
    {
        switch (targetId)
        {
            case RenderTargetId.Scene:
                var scenePass = (IScenePass)(_renderTargets[(int)RenderTargetId.Scene][0]);
                var sceneClear = scenePass.Clear!.Value;
                scenePass.Clear = sceneClear with { ClearColor = mutation.ClearColor!.Value };
                break;
            case RenderTargetId.Light:
                var lightPass = (LightRenderPass)(_renderTargets[(int)RenderTargetId.Light][0]);
                var lightClear = lightPass.Clear!.Value;
                lightPass.Clear = lightClear with { ClearColor = mutation.ClearColor!.Value };
                break;
            case RenderTargetId.Screen:
                break;
        }
    }

    public void RegisterRenderPass(RenderTargetId target, IRenderPassDescriptor pass)
    {
        if (pass.Op == RenderPassOp.Blit && pass is not BlitRenderPass)
            throw new InvalidOperationException($"RenderPass: Blit require {nameof(BlitRenderPass)}");

        _renderTargets[(int)target].Add(pass);
    }
    
    public void CreateSceneBuffer()
    {
        if (SceneFbo.IsValid)
            throw new InvalidOperationException("Scene buffer is already created");

        var desc = new FrameBufferDesc(SizeRatio: Vector2.One, AbsoluteSize: _snapshot.OutputSize, DepthStencilBuffer: true);
        var fboId = GfxAllocator.CreateFrameBuffer(in desc, out var meta);
        
        var layout = _graphics.Registry.FboRepository.Get(fboId);
        SceneFbo = RenderPassFboRecord.From(fboId, in meta, layout);
    }

    public void CreateMultisampleBuffer(Vector2 sizeRatio, uint samples)
    {
        ValidateSizeRatio(sizeRatio);
        if (samples is not (0 or 2 or 4 or 8))
            throw new ArgumentOutOfRangeException(nameof(samples), "Valid samples are 0,2,4,8");

        if (MultisampleFbo.IsValid)
            throw new InvalidOperationException("Multisample buffer is already created");

        var desc = new FrameBufferDesc(
            AbsoluteSize: _snapshot.OutputSize,
            SizeRatio: sizeRatio, DepthStencilBuffer: true, Msaa: samples > 0, Samples: samples);
        
        var fboId = GfxAllocator.CreateFrameBuffer(in desc, out var meta);

        var layout = _graphics.Registry.FboRepository.Get(fboId);
        MultisampleFbo = RenderPassFboRecord.From(fboId, in meta, layout);
    }
    
    public void CreateShadowBuffer(Vector2D<int> absoluteSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(absoluteSize.X, 64);
        ArgumentOutOfRangeException.ThrowIfLessThan( absoluteSize.Y, 64);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(absoluteSize.X, 4096);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(absoluteSize.Y, 4096);

        if (ShadowFbo.IsValid)
            throw new InvalidOperationException("Shadow buffer is already created");

        var desc = new FrameBufferDesc(SizeRatio:Vector2.One, AbsoluteSize: absoluteSize, DepthStencilBuffer: true);
        
        var fboId = GfxAllocator.CreateFrameBuffer(in desc , out var meta);

        var layout = _graphics.Registry.FboRepository.Get(fboId);
        ShadowFbo =  RenderPassFboRecord.From(fboId, in meta, layout);
    }

    public void CreateLightBuffer(Vector2 sizeRatio, TexturePreset preset)
    {
        ValidateSizeRatio(sizeRatio);
        if (LightFbo.IsValid)
            throw new InvalidOperationException("Light buffer is already created");

        var desc = new FrameBufferDesc(SizeRatio: sizeRatio,AbsoluteSize: _snapshot.OutputSize,
            TexturePreset: preset,
            DepthStencilBuffer: false);
        
        var fboId = GfxAllocator.CreateFrameBuffer(in desc , out var meta);

        var layout = _graphics.Registry.FboRepository.Get(fboId);
        LightFbo = RenderPassFboRecord.From(fboId, in meta, layout);
    }
    
    public void CreatePostProcessBuffer_A(Vector2 sizeRatio)
    {
        if (PostFboA.IsValid)
            throw new InvalidOperationException("Post Process buffer is already created");

        PostFboA= CreatePostProcessBuffer(sizeRatio);
    }
    
    public void CreatePostProcessBuffer_B(Vector2 sizeRatio)
    {
        if (PostFboB.IsValid)
            throw new InvalidOperationException("Post Process buffer is already created");

        PostFboB = CreatePostProcessBuffer(sizeRatio);
    }

    private RenderPassFboRecord CreatePostProcessBuffer(Vector2 sizeRatio)
    {
        ValidateSizeRatio(sizeRatio);

        var desc = new FrameBufferDesc(
            AbsoluteSize: _snapshot.OutputSize,
            SizeRatio:Vector2.One, 
            TexturePreset: TexturePreset.LinearClamp, 
            DepthStencilBuffer: false);
        
        var fboId = _graphics.Allocator.CreateFrameBuffer(in desc , out var meta);
        var layout = _graphics.Registry.FboRepository.Get(fboId);
        return RenderPassFboRecord.From(fboId, in meta, layout);
    }


    private void ValidateSizeRatio(Vector2 sizeRatio)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sizeRatio.X, 0, nameof(sizeRatio.X));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sizeRatio.Y, 0, nameof(sizeRatio.Y));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sizeRatio.X, 1, nameof(sizeRatio.X));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sizeRatio.Y, 1, nameof(sizeRatio.Y));
    }
}