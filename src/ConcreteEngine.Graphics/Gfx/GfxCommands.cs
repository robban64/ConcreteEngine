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

    //
    private GfxStateFlags _activeFlags;
    private GfxPassFunctions _passFunctions;


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
        _activeFlags = 0;
        _passFunctions = default;
    }

    internal void EndFrame()
    {
        UseShader(default);
        BindFramebuffer(default);

        Array.Clear(_boundTextures);
    }

    public void BeginScreenPass(GfxPassClear passClear, GfxPassState states)
    {
        BindFramebuffer(default);
        SetViewport(_activeOutputSize);
        ApplyPassState(states);

        Clear(passClear);

        _activeOutputSize = _outputSize;
    }


    public void BeginRenderPass(FrameBufferId fboId, GfxPassClear passClear, GfxPassState states)
    {
        ArgumentOutOfRangeException.ThrowIfZero(fboId.Id, nameof(fboId));
        if (_boundFboId == fboId) GraphicsException.ThrowInvalidState("FBO is already bound.", fboId);

        var size = _fboStore.GetMeta(fboId).Size;

        BindFramebuffer(fboId);
        SetViewport(size);
        ApplyPassState(states);
        Clear(passClear);

        _activeOutputSize = size;
    }

    public void EndRenderPass()
    {
        if (_boundFboId == default) GraphicsException.ResourceNotBound(nameof(_boundFboId));

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

    public void ApplyPassState(GfxPassState state)
    {
        var e = state.Enabled;

        _cmdStates.ToggleDepthMask((e & GfxStateFlags.DepthWrite) != 0);
        _cmdStates.ColorMask((e & GfxStateFlags.ColorMask) != 0);
        foreach (var flag in EnumCache<GfxStateFlags>.Values.AsSpan(3))
        {
            _cmdStates.ToggleStateFlag(flag, (e & flag) != 0);
        }

        _activeFlags = state.Enabled;
    }

    public void ApplyState(GfxPassState state)
    {
        var d = state.Defined;
        if (d == 0) return;
        var e = state.Enabled;

        var activeFlags = _activeFlags;

        _cmdStates.ToggleDepthMask((d & GfxStateFlags.DepthWrite) != 0 ? (e & GfxStateFlags.DepthWrite) != 0 : (activeFlags & GfxStateFlags.DepthWrite) != 0);
        _cmdStates.ColorMask((d & GfxStateFlags.ColorMask) != 0 ? (e & GfxStateFlags.ColorMask) != 0 : (activeFlags & GfxStateFlags.ColorMask) != 0);

        foreach (var flag in EnumCache<GfxStateFlags>.Values.AsSpan(3))
        {
            var enabled = (d & flag) != 0 ? (e & flag) != 0 : (activeFlags & flag) != 0;
            _cmdStates.ToggleStateFlag(flag, enabled);
        }
    }

    public void ApplyStateFunctions(GfxPassFunctions cmdFunc)
    {
        SetBlendMode(cmdFunc.Blend);
        SetCullMode(cmdFunc.Cull);
        SetDepthMode(cmdFunc.Depth);
        SetPolygonOffset(cmdFunc.PolygonOffset);
        _passFunctions = cmdFunc;
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
        if (_passFunctions.PolygonOffset != PolygonOffsetLevel.Unset && _passFunctions.PolygonOffset == polygon) return;
        var (factor, units) = polygon.ToFactorUnits();
        _passFunctions.PolygonOffset = polygon;
        _cmdStates.SetPolygonOffset(factor, units);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlendMode(BlendMode blendMode)
    {
        if (_passFunctions.Blend != BlendMode.Unset && _passFunctions.Blend == blendMode) return;
        _passFunctions.Blend = blendMode;
        _cmdStates.SetBlendMode(blendMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDepthMode(DepthMode depthMode)
    {
        if (_passFunctions.Depth != DepthMode.Unset && _passFunctions.Depth == depthMode) return;
        _passFunctions.Depth = depthMode;
        _cmdStates.SetDepthMode(depthMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCullMode(CullMode cullMode)
    {
        if (_passFunctions.Cull != CullMode.Unset && _passFunctions.Cull == cullMode) return;
        _passFunctions.Cull = cullMode;
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