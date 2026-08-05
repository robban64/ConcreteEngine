using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.OpenGL;
using static ConcreteEngine.Graphics.Gfx.GfxRegistry;
using static ConcreteEngine.Graphics.Gfx.GfxStateFlags;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxCommands
{
    //States
    private static InlineArray16<TextureId> _boundTextures;
    private static InlineArray16<SamplerProfile> _boundSamplers;

    private MeshId _boundMeshId;
    private ShaderId _boundShaderId;
    private FrameBufferId _boundFboId;

    private Size2D _outputSize;
    private Size2D _activeOutputSize;

    private GfxStateFlags _passFlags;
    private GfxDrawFunctions _stateFunctions;

    private GfxDrawState _lastDrawState;

    internal GfxCommands()
    {
        SetBlendMode(BlendMode.Alpha);
        SetDepthMode(DepthMode.Lequal);
        SetCullMode(CullMode.BackCcw);
    }

    internal void BeginFrame(Size2D outputSize)
    {
        _outputSize = outputSize;
        _activeOutputSize = outputSize;
        _passFlags = default;
        _stateFunctions = default;
        _lastDrawState = default;
    }

    internal void EndFrame()
    {
        _boundMeshId = default;
        GlStates.BindMesh(default);

        UseShader(default);
        BindFramebuffer(default);

        _boundTextures = default;
        _boundSamplers = default;
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

        var size = FboStore.GetMeta(fboId).Size;

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

        var fromHandle = FboStore.GetHandleAndMeta(fromId, out var fromMeta);
        var toHandle = FboStore.TryGet(toId, out _);

        if (!toHandle.IsValid())
        {
            GlFrameBuffers.BlitDefault(fromHandle, fromMeta.Size, _activeOutputSize, false);
            return;
        }

        GlFrameBuffers.Blit(fromHandle, toHandle, fromMeta.Size, fromMeta.Size, linear);
    }
    public void GenerateMipMaps(TextureId textureId)
    {
        var texHandle = TextureStore.GetHandleAndMeta(textureId, out var meta);
        Debug.Assert(meta.MipLevels > 1);
        GlTextures.GenerateMipMaps(texHandle);
    }


    public void Clear(ColorRgba clearColor, ClearBufferFlag clearFlag)
    {
        switch (clearFlag)
        {
            case ClearBufferFlag.Color: GlStates.ClearColor(clearColor); break;
            case ClearBufferFlag.Depth: GlStates.ClearBuffer(clearFlag); break;
            case ClearBufferFlag.ColorAndDepth:
                GlStates.ClearColor(clearColor);
                GlStates.ClearBuffer(clearFlag);
                break;
        }
    }

    public void ApplyPassState(GfxStateFlags e)
    {
        GlStates.ToggleDepthTest((e & DepthTest) != 0);
        GlStates.ToggleDepthMask((e & DepthWrite) != 0);
        GlStates.ToggleCullFace((e & Cull) != 0);
        GlStates.ToggleBlendState((e & Blend) != 0);
        GlStates.TogglePolygonOffset((e & PolygonOffset) != 0);
        GlStates.ToggleSampleAlphaCoverage((e & Ac2) != 0);

        GlStates.ToggleFrameBufferSrgb((e & Srgb) != 0);
        GlStates.ColorMask((e & ColorMask) != 0);
        GlStates.ToggleScissorTest((e & Scissor) != 0);

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
        GlStates.ToggleDepthTest((d & DepthTest) != 0 ? (e & DepthTest) != 0 : (p & DepthTest) != 0);
        GlStates.ToggleDepthMask((d & DepthWrite) != 0 ? (e & DepthWrite) != 0 : (p & DepthWrite) != 0);
        GlStates.ToggleCullFace((d & Cull) != 0 ? (e & Cull) != 0 : (p & Cull) != 0);
        GlStates.ToggleBlendState((d & Blend) != 0 ? (e & Blend) != 0 : (p & Blend) != 0);
        GlStates.TogglePolygonOffset((d & PolygonOffset) != 0 ? (e & PolygonOffset) != 0 : (p & PolygonOffset) != 0);
        GlStates.ToggleSampleAlphaCoverage((d & Ac2) != 0 ? (e & Ac2) != 0 : (p & Ac2) != 0);
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
        GlStates.SetViewport(viewportSize);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPolygonOffset(PolygonOffsetLevel polygon)
    {
        if (_stateFunctions.PolygonOffset != PolygonOffsetLevel.Unset &&
            _stateFunctions.PolygonOffset == polygon) return;
        var (factor, units) = polygon.ToFactorUnits();
        _stateFunctions.PolygonOffset = polygon;
        GlStates.SetPolygonOffset(factor, units);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlendMode(BlendMode blendMode)
    {
        if (_stateFunctions.Blend != BlendMode.Unset && _stateFunctions.Blend == blendMode) return;
        _stateFunctions.Blend = blendMode;
        GlStates.SetBlendMode(blendMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDepthMode(DepthMode depthMode)
    {
        if (_stateFunctions.Depth != DepthMode.Unset && _stateFunctions.Depth == depthMode) return;
        _stateFunctions.Depth = depthMode;
        GlStates.SetDepthMode(depthMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCullMode(CullMode cullMode)
    {
        if (_stateFunctions.Cull != CullMode.Unset && _stateFunctions.Cull == cullMode) return;
        _stateFunctions.Cull = cullMode;
        GlStates.SetCullMode(cullMode);
    }

    public void BindFramebuffer(FrameBufferId id)
    {
        if (_boundFboId == id) return;
        if (id == default)
        {
            GlStates.UnbindFrameBuffer();
            _boundFboId = default;
            return;
        }

        GlStates.BindFrameBuffer(FboStore.GetHandle(id));
        _boundFboId = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindTextureAndSampler(TextureId texture, SamplerProfile sampler, int slot)
    {
        Debug.Assert(slot >= 0 && slot <= GfxLimits.TextureSlots);
        BindSampler(sampler, slot);
        BindTextureSlot(texture, slot);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindTextureSlot(TextureId texture, int slot)
    {
        if (_boundTextures[slot] == texture) return;
        _boundTextures[slot] = texture;
        var textureHandle = texture > 0 ? TextureStore.GetHandle(texture) : default;
        GlStates.BindTexture(textureHandle, slot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindSampler(SamplerProfile sampler, int slot)
    {
        if (_boundSamplers[slot] == sampler) return;
        _boundSamplers[slot] = sampler;
        GlStates.BindSampler(GfxTextures.GetSamplerHandle(sampler), slot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindAllTextures()
    {
        _boundTextures = default;
        GlStates.UnbindAllTextures();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UseShader(ShaderId id)
    {
        if (_boundShaderId == id) return;

        if (id == default)
        {
            _boundShaderId = default;
            GlStates.UnbindShader();
            return;
        }

        var handle = ShaderStore.GetHandle(id);
        GlStates.UseShader(handle);
        _boundShaderId = id;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindUniformBufferRange<T>(int offset, int length) where T : unmanaged, IUniform
    {
        var id = UboStore.GetHandle(T.UboId);
        GlBuffers.BindUniformBufferRange(id, T.Slot, offset * T.Stride, length * T.Stride);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawMesh(MeshId id)
    {
        if (_boundMeshId != id)
        {
            _boundMeshId = id;
            GlStates.BindMesh(MeshStore.GetHandle(id));
        }

        var meta = MeshStore.GetMeta(id);
        GlStates.Draw(meta.Primitive, meta.ElementSize, meta.DrawCount);
        GfxMetrics.AddDrawCall(meta.DrawCount, 0);
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawMeshInstanced(MeshId id, uint instanceCount)
    {
        if (_boundMeshId != id)
        {
            _boundMeshId = id;
            GlStates.BindMesh(MeshStore.GetHandle(id));
        }

        var meta = MeshStore.GetMeta(id);
        instanceCount = uint.Max(meta.InstanceCount, instanceCount);
        GlStates.DrawInstance(meta.Primitive, meta.ElementSize, meta.DrawCount, instanceCount);
        GfxMetrics.AddDrawCall(meta.DrawCount, instanceCount);
    }
}