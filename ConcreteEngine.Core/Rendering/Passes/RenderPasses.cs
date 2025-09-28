using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;
/*
public struct RenderPassMutation
{
    public Color4? ClearColor;
}

internal class RenderPasses
{
    public FrameBufferLayout MultisampleFbo { get; private set; }
    public FrameBufferLayout SceneFbo { get; private set; }
    public FrameBufferLayout LightFbo { get; private set; }
    public FrameBufferLayout ShadowFbo { get; private set; }
    public FrameBufferLayout PostFboA { get; private set; }
    public FrameBufferLayout PostFboB { get; private set; }


    private readonly GfxContext _gfx;
    private readonly RenderRegistry _renderRegistry;

    private readonly RenderGlobalSnapshot _snapshot;

    private readonly List<IRenderPassDescriptor>[] _renderTargets;

    private int _currentTargetId = 0;

    private Size2D SnapShotOutput => new(_snapshot.OutputSize.X, _snapshot.OutputSize.Y);

    public RenderPasses(GfxContext gfx, in RenderGlobalSnapshot snapshot)
    {
        _gfx = gfx;
        _snapshot = snapshot;

        _renderTargets = new List<IRenderPassDescriptor>[RenderConsts.RenderTargetCount];
        for (int i = 0; i < RenderConsts.RenderTargetCount; i++)
        {
            _renderTargets[i] = new List<IRenderPassDescriptor>(4);
        }
    }

    public bool TryGetNextPasses(out RenderTargetId targetId, out List<IRenderPassDescriptor> passes)
    {
        if (_currentTargetId >= RenderConsts.RenderTargetCount)
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
                var sceneClear = scenePass.Clear;
                scenePass.Clear = sceneClear with { ClearColor = mutation.ClearColor!.Value };
                break;
            case RenderTargetId.Light:
                var lightPass = (LightRenderPass)(_renderTargets[(int)RenderTargetId.Light][0]);
                var lightClear = lightPass.Clear;
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
        if (SceneFbo.FboId.IsValid())
            throw new InvalidOperationException("Scene buffer is already created");


        var entry = new RegisterFboEntry().AttachColorTexture().AttachDepthStencilBuffer();
        _renderRegistry.RegisterFrameBuffer<FboSceneTag>(entry);

        var desc = new GfxFrameBufferDescriptor(
            Size: SnapShotOutput,
            PixelFormat: EnginePixelFormat.Rgba,
            Attachments: new GfxFrameBufferDescriptor.AttachmentsDef(true, false, false, true)
        );
        SceneFbo = _gfxFbo.RegisterFrameBufferScreen(desc);
    }

    public void CreateMultisampleBuffer(Vector2 sizeRatio, int samples)
    {
        ValidateSizeRatio(sizeRatio);
        if (samples is not (0 or 2 or 4 or 8))
            throw new ArgumentOutOfRangeException(nameof(samples), "Valid samples are 0,2,4,8");

        if (MultisampleFbo.FboId.IsValid())
            throw new InvalidOperationException("Multisample buffer is already created");

        var entry = RegisterFboEntry.MakeMsaa((RenderBufferMsaa)samples)
            .AttachColorTexture().AttachDepthStencilBuffer();
        _renderRegistry.RegisterFrameBuffer<FboMsaaTag>(entry);

        var desc = new GfxFrameBufferDescriptor(
            Size: SnapShotOutput,
            Multisample: (RenderBufferMsaa)samples,
            Attachments: new GfxFrameBufferDescriptor.AttachmentsDef(true, false, false, true),
            TexturePreset: TexturePreset.None
        );

        MultisampleFbo = _gfxFbo.RegisterFrameBufferScreen(desc);
    }

    public void CreateShadowBuffer(Size2D size)
    {
        if (ShadowFbo.FboId.IsValid())
            throw new InvalidOperationException("Shadow buffer is already created");

        var desc = new GfxFrameBufferDescriptor(
            Size: size,
            Attachments: new GfxFrameBufferDescriptor.AttachmentsDef(true, false, false, true)
        );

        //ShadowFbo = _gfxFbo.RegisterFrameBufferFixed(desc, size);
    }

    public void CreateLightBuffer(Vector2 sizeRatio, TexturePreset preset)
    {
        ValidateSizeRatio(sizeRatio);
        if (LightFbo.FboId.IsValid())
            throw new InvalidOperationException("Light buffer is already created");

        var desc = new GfxFrameBufferDescriptor(
            Size: SnapShotOutput.Scale(sizeRatio),
            Attachments: new GfxFrameBufferDescriptor.AttachmentsDef(true, false, false, false)
        );


        LightFbo = _gfxFbo.RegisterFrameBufferCalc(desc, sizeRatio,
            (outputSize, ratio) => outputSize.Scale(ratio)
    }

    public void CreatePostProcessBuffer_A()
    {
        if (PostFboA.FboId.IsValid())
            throw new InvalidOperationException("Post Process buffer is already created");


        PostFboA = CreatePostProcessBuffer(true);
    }

    public void CreatePostProcessBuffer_B()
    {
        if (PostFboB.FboId.IsValid())
            throw new InvalidOperationException("Post Process buffer is already created");

        PostFboB = CreatePostProcessBuffer(false);
    }

    private FrameBufferLayout CreatePostProcessBuffer(bool mipmap)
    {
        var entry = RegisterFboEntry.MakePost(mipmap).AttachColorTexture();
        _renderRegistry.RegisterFrameBuffer<FboPostProcessTag>(entry);

        var desc = new GfxFrameBufferDescriptor(
            Size: SnapShotOutput,
            TexturePreset: mipmap ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp,
            Attachments: new GfxFrameBufferDescriptor.AttachmentsDef(true, false, false, false)
        );
        return _gfxFbo.RegisterFrameBufferScreen(desc);
    }


    private static void ValidateSizeRatio(Vector2 sizeRatio)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sizeRatio.X, 0, nameof(sizeRatio.X));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sizeRatio.Y, 0, nameof(sizeRatio.Y));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sizeRatio.X, 1, nameof(sizeRatio.X));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sizeRatio.Y, 1, nameof(sizeRatio.Y));
    }
}*/