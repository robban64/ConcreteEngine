using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using static ConcreteEngine.Graphics.Gfx.GfxStateFlags;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxCommands
{
    private static Size2D _outputSize;
    private static Size2D _activeOutputSize;

    private readonly GlStates _cmdStates;
    private readonly GlFrameBuffers _frameBuffers;

    private readonly FboStore _fboStore;
    private readonly TextureStore _textureStore;
    private readonly ShaderStore _shaderStore;

    //States
    private readonly TextureId[] _boundTextures = new TextureId[GfxLimits.TextureSlots];

    private FrameBufferId _boundFboId;
    private ShaderId _boundShaderId;

    public GfxStateFlags PassState;
    public GfxPassFunctions PassFunctions;


    internal GfxCommands(GfxContextInternal ctx)
    {
        _cmdStates = ctx.Driver.States;
        _frameBuffers = ctx.Driver.FrameBuffers;

        _fboStore = GfxRegistry.GetGfxStore<FrameBufferMeta>();
        _textureStore = GfxRegistry.GetGfxStore<TextureMeta>();
        _shaderStore = GfxRegistry.GetGfxStore<ShaderMeta>();

        SetBlendMode(BlendMode.Alpha);
        SetDepthMode(DepthMode.Lequal);
        SetCullMode(CullMode.BackCcw);
    }

    internal void BeginFrame(GfxFrameArgs args)
    {
        _outputSize = args.OutputSize;
        _activeOutputSize = args.OutputSize;
        PassState = default;
        PassFunctions = default;
    }

    internal void EndFrame()
    {
        UseShader(default);
        BindFramebuffer(default);

        Array.Clear(_boundTextures);
    }

    public void BeginScreenPass(GfxPassClear passClear, GfxStateFlags stateFlags)
    {
        BindFramebuffer(default);
        SetViewport(_activeOutputSize);
        ApplyPassState(stateFlags);

        Clear(passClear);

        _activeOutputSize = _outputSize;
    }


    public void BeginRenderPass(FrameBufferId fboId, GfxPassClear passClear, GfxStateFlags stateFlags)
    {
        ArgumentOutOfRangeException.ThrowIfZero(fboId.Id, nameof(fboId));
        if (_boundFboId == fboId) GraphicsException.ThrowInvalidState("FBO is already bound.", fboId);

        var size = _fboStore.GetMeta(fboId).Size;

        BindFramebuffer(fboId);
        SetViewport(size);
        ApplyPassState(stateFlags);
        Clear(passClear);

        _activeOutputSize = size;
    }

    public void EndRenderPass()
    {
        if (_boundFboId == default) GraphicsException.ResourceNotBound(nameof(_boundFboId));
        PassState = default;
        PassFunctions = default;

        BindFramebuffer(default);

        _activeOutputSize = _outputSize;
        SetViewport(_activeOutputSize);
    }


    public void BlitFramebuffer(FrameBufferId fromId, FrameBufferId toId, bool linear)
    {
        Debug.Assert(fromId != default);
        Debug.Assert(fromId != toId, "READ and DRAW FBO must differ for resolve.");

        var fromHandle = _fboStore.GetHandleAndMeta(fromId, out var fromMeta);
        var toHandle = _fboStore.TryGet(toId, out _);

        if (!toHandle.IsValid)
        {
            _frameBuffers.BlitDefault(fromHandle, fromMeta.Size, _activeOutputSize, false);
            return;
        }

        _frameBuffers.Blit(fromHandle, toHandle, fromMeta.Size, fromMeta.Size, linear);
    }


    public void Clear(GfxPassClear passClear)
    {
        switch (passClear.ClearBuffer)
        {
            case ClearBufferFlag.Color: _cmdStates.ClearColor(passClear.ClearColor); break;
            case ClearBufferFlag.Depth: _cmdStates.ClearBuffer(passClear.ClearBuffer); break;
            case ClearBufferFlag.ColorAndDepth:
                _cmdStates.ClearColor(passClear.ClearColor);
                _cmdStates.ClearBuffer(passClear.ClearBuffer);
                break;
        }
    }

    public void ApplyPassState(GfxStateFlags e)
    {
        _cmdStates.ColorMask((e & ColorMask) != 0);
        _cmdStates.ToggleScissorTest((e & Scissor) != 0);
        _cmdStates.ToggleCullFace((e & Cull) != 0 );
        _cmdStates.ToggleDepthTest((e & DepthTest) != 0 );
        _cmdStates.ToggleDepthMask((e & DepthWrite) != 0 );
        _cmdStates.ToggleBlendState((e & Blend) != 0 );
        _cmdStates.ToggleFrameBufferSrgb((e & Srgb) != 0 );
        _cmdStates.TogglePolygonOffset((e & PolygonOffset) != 0 );
        _cmdStates.ToggleSampleAlphaCoverage((e & Ac2) != 0 );

        PassState = e;
    }

    public void ApplyState(GfxDrawState state)
    {
        var d = (GfxStateFlags)state.Defined;
        if (d == 0) return;
        var e = (GfxStateFlags)state.Enabled;

        var p = PassState;
        _cmdStates.ToggleCullFace((d & Cull) != 0 ? (e & Cull) != 0 : (p & Cull) != 0);
        _cmdStates.ToggleDepthTest((d & DepthTest) != 0 ? (e & DepthTest) != 0 : (p & DepthTest) != 0);
        _cmdStates.ToggleDepthMask((d & DepthWrite) != 0 ? (e & DepthWrite) != 0 : (p & DepthWrite) != 0);
        _cmdStates.ToggleBlendState((d & Blend) != 0 ? (e & Blend) != 0 : (p & Blend) != 0);
        _cmdStates.TogglePolygonOffset((d & PolygonOffset) != 0 ? (e & PolygonOffset) != 0 : (p & PolygonOffset) != 0);
        _cmdStates.ToggleSampleAlphaCoverage((d & Ac2) != 0 ? (e & Ac2) != 0 : (p & Ac2) != 0);

    }

    public void ApplyStateFunctions(GfxPassFunctions cmdFunc)
    {
        SetBlendMode(cmdFunc.Blend);
        SetCullMode(cmdFunc.Cull);
        SetDepthMode(cmdFunc.Depth);
        SetPolygonOffset(cmdFunc.PolygonOffset);
        PassFunctions = cmdFunc;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetViewport(Size2D viewportSize)
    {
        _activeOutputSize = viewportSize;
        _cmdStates.SetViewport(viewportSize);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPolygonOffset(PolygonOffsetLevel polygon)
    {
        if (PassFunctions.PolygonOffset != PolygonOffsetLevel.Unset && PassFunctions.PolygonOffset == polygon) return;
        var (factor, units) = polygon.ToFactorUnits();
        PassFunctions.PolygonOffset = polygon;
        _cmdStates.SetPolygonOffset(factor, units);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlendMode(BlendMode blendMode)
    {
        if (PassFunctions.Blend != BlendMode.Unset && PassFunctions.Blend == blendMode) return;
        PassFunctions.Blend = blendMode;
        _cmdStates.SetBlendMode(blendMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDepthMode(DepthMode depthMode)
    {
        if (PassFunctions.Depth != DepthMode.Unset && PassFunctions.Depth == depthMode) return;
        PassFunctions.Depth = depthMode;
        _cmdStates.SetDepthMode(depthMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCullMode(CullMode cullMode)
    {
        if (PassFunctions.Cull != CullMode.Unset && PassFunctions.Cull == cullMode) return;
        PassFunctions.Cull = cullMode;
        _cmdStates.SetCullMode(cullMode);
    }

    public void BindFramebuffer(FrameBufferId id)
    {
        if (_boundFboId == id) return;
        if (id == default)
        {
            _cmdStates.UnbindFrameBuffer();
            _boundFboId = default;
            return;
        }

        _cmdStates.BindFrameBuffer(_fboStore.GetHandle(id));
        _boundFboId = id;
    }

    public void BindTexture(TextureId texture, int slot)
    {
        Debug.Assert(slot >= 0 && slot <= GfxLimits.TextureSlots);
        ref var boundTexture = ref _boundTextures[slot];
        if (boundTexture == texture) return;
        boundTexture = texture;

        if (boundTexture == 0)
        {
            _cmdStates.UnbindTextureSlot(slot);
            return;
        }

        var refHandle = _textureStore.GetHandle(boundTexture);
        _cmdStates.BindTexture(refHandle, slot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindAllTextures()
    {
        Array.Clear(_boundTextures);
        _cmdStates.UnbindAllTextures();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UseShader(ShaderId id)
    {
        if (_boundShaderId == id) return;

        if (id == default)
        {
            _boundShaderId = default;
            _cmdStates.UnbindShader();
            return;
        }

        var handle = _shaderStore.GetHandle(id);
        _cmdStates.UseShader(handle);
        _boundShaderId = id;
    }
}