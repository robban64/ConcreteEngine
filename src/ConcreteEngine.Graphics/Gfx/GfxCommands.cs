using System.Diagnostics;
using System.Runtime.CompilerServices;
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

    private GfxStateFlags _passFlags;
    private GfxDrawFunctions _stateFunctions;

    private GfxDrawState _lastDrawState;


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
        _passFlags = default;
        _stateFunctions = default;
        _lastDrawState = default;
    }

    internal void EndFrame()
    {
        UseShader(default);
        BindFramebuffer(default);

        Array.Clear(_boundTextures);
    }

    public void BeginScreenPass(GfxPassState passState)
    {
        BindFramebuffer(default);
        SetViewport(_activeOutputSize);
        ApplyPassState(passState.StateFlags);

        Clear(passState.ClearColor, passState.ClearBuffer);

        _activeOutputSize = _outputSize;
        _lastDrawState = default;
    }


    public void BeginRenderPass(FrameBufferId fboId, GfxPassState passState)
    {
        ArgumentOutOfRangeException.ThrowIfZero(fboId.Id, nameof(fboId));
        if (_boundFboId == fboId) GraphicsException.ThrowInvalidState("FBO is already bound.", fboId);

        var size = _fboStore.GetMeta(fboId).Size;

        BindFramebuffer(fboId);
        SetViewport(size);
        ApplyPassState(passState.StateFlags);
        Clear(passState.ClearColor, passState.ClearBuffer);

        _activeOutputSize = size;
        _lastDrawState = default;
    }

    public void EndRenderPass()
    {
        if (_boundFboId == default) GraphicsException.ResourceNotBound(nameof(_boundFboId));
        _passFlags = default;
        _stateFunctions = default;

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


    public void Clear(ColorRgba clearColor, ClearBufferFlag clearFlag)
    {
        switch (clearFlag)
        {
            case ClearBufferFlag.Color: _cmdStates.ClearColor(clearColor); break;
            case ClearBufferFlag.Depth: _cmdStates.ClearBuffer(clearFlag); break;
            case ClearBufferFlag.ColorAndDepth:
                _cmdStates.ClearColor(clearColor);
                _cmdStates.ClearBuffer(clearFlag);
                break;
        }
    }

    public void ApplyPassState(GfxStateFlags e)
    {
        _cmdStates.ToggleDepthTest((e & DepthTest) != 0);
        _cmdStates.ToggleDepthMask((e & DepthWrite) != 0);
        _cmdStates.ToggleCullFace((e & Cull) != 0);
        _cmdStates.ToggleBlendState((e & Blend) != 0);
        _cmdStates.TogglePolygonOffset((e & PolygonOffset) != 0);
        _cmdStates.ToggleSampleAlphaCoverage((e & Ac2) != 0);

        _cmdStates.ToggleFrameBufferSrgb((e & Srgb) != 0);
        _cmdStates.ColorMask((e & ColorMask) != 0);
        _cmdStates.ToggleScissorTest((e & Scissor) != 0);

        _passFlags = e;
    }

    public void ApplyState(GfxDrawState state)
    {
        if (_lastDrawState == state) return;
        _lastDrawState = state;

        var d = (GfxStateFlags)state.Defined;
        if (d == 0) return;
        var e = (GfxStateFlags)state.Enabled;

        var p = _passFlags;
        _cmdStates.ToggleDepthTest((d & DepthTest) != 0 ? (e & DepthTest) != 0 : (p & DepthTest) != 0);
        _cmdStates.ToggleDepthMask((d & DepthWrite) != 0 ? (e & DepthWrite) != 0 : (p & DepthWrite) != 0);
        _cmdStates.ToggleCullFace((d & Cull) != 0 ? (e & Cull) != 0 : (p & Cull) != 0);
        _cmdStates.ToggleBlendState((d & Blend) != 0 ? (e & Blend) != 0 : (p & Blend) != 0);
        _cmdStates.TogglePolygonOffset((d & PolygonOffset) != 0 ? (e & PolygonOffset) != 0 : (p & PolygonOffset) != 0);
        _cmdStates.ToggleSampleAlphaCoverage((d & Ac2) != 0 ? (e & Ac2) != 0 : (p & Ac2) != 0);
    }

    public void ApplyStateFunctions(GfxDrawFunctions stateFunctions)
    {
        if (_stateFunctions == stateFunctions) return;

        SetBlendMode(stateFunctions.Blend);
        SetCullMode(stateFunctions.Cull);
        SetDepthMode(stateFunctions.Depth);
        SetPolygonOffset(stateFunctions.PolygonOffset);
        _stateFunctions = stateFunctions;
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
        if (_stateFunctions.PolygonOffset != PolygonOffsetLevel.Unset &&
            _stateFunctions.PolygonOffset == polygon) return;
        var (factor, units) = polygon.ToFactorUnits();
        _stateFunctions.PolygonOffset = polygon;
        _cmdStates.SetPolygonOffset(factor, units);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlendMode(BlendMode blendMode)
    {
        if (_stateFunctions.Blend != BlendMode.Unset && _stateFunctions.Blend == blendMode) return;
        _stateFunctions.Blend = blendMode;
        _cmdStates.SetBlendMode(blendMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDepthMode(DepthMode depthMode)
    {
        if (_stateFunctions.Depth != DepthMode.Unset && _stateFunctions.Depth == depthMode) return;
        _stateFunctions.Depth = depthMode;
        _cmdStates.SetDepthMode(depthMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCullMode(CullMode cullMode)
    {
        if (_stateFunctions.Cull != CullMode.Unset && _stateFunctions.Cull == cullMode) return;
        _stateFunctions.Cull = cullMode;
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