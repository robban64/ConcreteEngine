using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.OpenGL.Utilities;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxCommands
{
    private readonly IGraphicsDriver _driver;
    private readonly GlStates _states;
    private readonly GlShaders _shaders;
    private readonly GlTextures _textures;
    private readonly GlFrameBuffers _frameBuffers;

    private readonly FboStore _fboStore;
    private readonly TextureStore _textureStore;
    private readonly MeshStore _meshStore;
    private readonly ShaderStore _shaderStore;


    //States
    private GfxStateFlags _activeFlags;
    private GfxPassStateFunc _stateFunc;

    private readonly TextureId[] _boundTextures;

    private FrameBufferId _boundFboId;

    private MeshId _boundMeshId;
    private MeshMeta _boundMeshMeta;

    private ShaderId _boundShaderId;
    private readonly int[] _boundUniforms = new int[GfxLimits.MaxPlainUniforms];

    //
    private Size2D _activeOutputSize;
    private GfxFrameArgs _frameArgs;

    private int _drawTriangleCount;
    private int _drawCallCount;
    private int _drawInstanceCount;


    internal GfxCommands(GfxContextInternal ctx)
    {
        _driver = ctx.Driver;
        _states = ctx.Driver.States;
        _shaders = ctx.Driver.Shaders;
        _textures = ctx.Driver.Textures;
        _frameBuffers = ctx.Driver.FrameBuffers;

        _fboStore = ctx.Resources.GfxStoreHub.FboStore;
        _textureStore = ctx.Resources.GfxStoreHub.TextureStore;
        _meshStore = ctx.Resources.GfxStoreHub.MeshStore;
        _shaderStore = ctx.Resources.GfxStoreHub.ShaderStore;

        _boundTextures = new TextureId[GfxLimits.TextureSlots];

        SetBlendMode(BlendMode.Alpha);
        SetDepthMode(DepthMode.Lequal);
        SetCullMode(CullMode.BackCcw);
    }

    internal void BeginFrame(in GfxFrameArgs frameCtx)
    {
        _frameArgs = frameCtx;

        _drawCallCount = 0;
        _drawTriangleCount = 0;

        _activeOutputSize = _frameArgs.OutputSize;
    }

    internal void EndFrame(out RenderFrameMeta result)
    {
        result = new RenderFrameMeta(_drawCallCount, _drawTriangleCount, _drawInstanceCount);
        UseShader(default);
        BindMesh(default);
        BindFramebuffer(default);

        //_stateFunc = new GfxPassStateFunc(BlendMode.Unset, CullMode.Unset, DepthMode.Unset);

        _boundTextures.AsSpan().Clear();
    }

    public void BeginScreenPass(in GfxPassClear passClear, GfxPassState states)
    {
        BindFramebuffer(default);
        SetViewport(_activeOutputSize);

        ApplyState(states);
        Clear(in passClear);

        _activeOutputSize = _frameArgs.OutputSize;
    }


    public void BeginRenderPass(FrameBufferId fboId, in GfxPassClear passClear, GfxPassState states)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fboId.Value, nameof(fboId));
        if (_boundFboId == fboId) GraphicsException.ThrowInvalidState($"FBO is {fboId} already bound.");

        ref readonly var meta = ref _fboStore.GetMeta(fboId);

        BindFramebuffer(fboId);
        SetViewport(meta.Size);
        ApplyState(states);
        Clear(in passClear);

        /*
        if (meta.Attachments.DepthTextureId != default)
        {
            _frameBuffers.SetDrawReadBuffer(_fboStore.GetRefHandle(fboId), false);
        }
        */

        _activeOutputSize = meta.Size;
    }

    public void EndRenderPass()
    {
        if (_boundFboId == default) GraphicsException.ResourceNotBound(nameof(_boundFboId));

        BindFramebuffer(default);
        _activeOutputSize = _frameArgs.OutputSize;

        SetViewport(_activeOutputSize);
    }


    public void BlitFramebuffer(FrameBufferId fromId, FrameBufferId toId, bool linear)
    {
        Debug.Assert(fromId != default);
        Debug.Assert(fromId != toId, "READ and DRAW FBO must differ for resolve.");

        var fromHandle = _fboStore.GetRefAndMeta(fromId, out var fromMeta);
        var srcSize = fromMeta.Size;

        var toHandle = _fboStore.TryGetRef(toId, out var fboView);

        if (!toHandle.IsValid)
        {
            _frameBuffers.BlitDefault(fromHandle, srcSize, _activeOutputSize, false);
            return;
        }

        _frameBuffers.Blit(fromHandle, toHandle, srcSize, fromMeta.Size, linear);
    }


    public void Clear(in GfxPassClear passClear)
    {
        if (passClear.ClearBuffer is ClearBufferFlag.Color or ClearBufferFlag.ColorAndDepth)
            _states.ClearColor(in passClear.ClearColor);

        if (passClear.ClearBuffer is not ClearBufferFlag.None)
            _states.ClearBuffer(passClear.ClearBuffer);
    }


    public void ApplyState(GfxPassState state)
    {
        var d = state.Defined;
        if (d == 0) return;
        var e = state.Enabled;

        if ((d & GfxStateFlags.Scissor) != 0) _states.ToggleScissorTest((e & GfxStateFlags.Scissor) != 0);
        if ((d & GfxStateFlags.Cull) != 0) _states.ToggleCullFace((e & GfxStateFlags.Cull) != 0);
        if ((d & GfxStateFlags.DepthTest) != 0) _states.ToggleDepthTest((e & GfxStateFlags.DepthTest) != 0);
        if ((d & GfxStateFlags.DepthWrite) != 0) _states.ToggleDepthMask((e & GfxStateFlags.DepthWrite) != 0);
        if ((d & GfxStateFlags.Blend) != 0) _states.ToggleBlendState((e & GfxStateFlags.Blend) != 0);
        if ((d & GfxStateFlags.FramebufferSrgb) != 0)
            _states.ToggleFrameBufferSrgb((e & GfxStateFlags.FramebufferSrgb) != 0);
        if ((d & GfxStateFlags.ColorMask) != 0) _states.ColorMask((e & GfxStateFlags.ColorMask) != 0);
        if ((d & GfxStateFlags.PolygonOffset) != 0) _states.TogglePolygonOffset((e & GfxStateFlags.PolygonOffset) != 0);
        if ((d & GfxStateFlags.SampleAlphaCoverage) != 0)
            _states.ToggleSampleAlphaCoverage((e & GfxStateFlags.SampleAlphaCoverage) != 0);

        _activeFlags = GfxPassState.Merge(_activeFlags, state);
    }

    public void ApplyStateFunctions(GfxPassStateFunc cmdFunc)
    {
        SetBlendMode(cmdFunc.Blend);
        SetCullMode(cmdFunc.Cull);
        SetDepthMode(cmdFunc.Depth);
        SetPolygonOffset(cmdFunc.PolygonOffset);
        _stateFunc = cmdFunc;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetViewport(Size2D viewportSize)
    {
        _activeOutputSize = viewportSize;
        _states.SetViewport(_activeOutputSize.ToBounds2D());
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPolygonOffset(PolygonOffsetLevel polygon)
    {
        if (_stateFunc.PolygonOffset != PolygonOffsetLevel.Unset && _stateFunc.PolygonOffset == polygon) return;

        (float factor, float units) = polygon.ToFactorUnits();
        _stateFunc = _stateFunc with { PolygonOffset = polygon };
        _states.SetPolygonOffset(factor, units);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlendMode(BlendMode blendMode)
    {
        if (_stateFunc.Blend != BlendMode.Unset && _stateFunc.Blend == blendMode) return;
        _stateFunc = _stateFunc with { Blend = blendMode };
        _states.SetBlendMode(blendMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDepthMode(DepthMode depthMode)
    {
        if (_stateFunc.Depth != DepthMode.Unset && _stateFunc.Depth == depthMode) return;
        _stateFunc = _stateFunc with { Depth = depthMode };
        _states.SetDepthMode(depthMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCullMode(CullMode cullMode)
    {
        if (_stateFunc.Cull != CullMode.Unset && _stateFunc.Cull == cullMode) return;
        _stateFunc = _stateFunc with { Cull = cullMode };
        _states.SetCullMode(cullMode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindFramebuffer(FrameBufferId id)
    {
        if (_boundFboId == id) return;
        if (id == default)
        {
            _states.UnbindFrameBuffer();
            _boundFboId = default;
            return;
        }

        _states.BindFrameBuffer(_fboStore.GetRefHandle(id));
        _boundFboId = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindTexture(TextureId texture, int slot)
    {
        Debug.Assert(slot >= 0 && slot <= GfxLimits.TextureSlots);

        if (_boundTextures[slot] == texture) return;
        _boundTextures[slot] = texture;
        if (texture.Value == 0)
        {
            _states.UnbindTextureSlot(slot);
            return;
        }

        var refHandle = _textureStore.GetRefHandle(texture);
        _states.BindTexture(refHandle, slot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindAllTextures() => _states.UnbindAllTextures();


    public void UseShader(ShaderId id, ReadOnlySpan<int> uniforms)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Value, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(uniforms.Length, GfxLimits.MaxPlainUniforms);
        if (_boundShaderId == id) return;

        var handle = _shaderStore.GetRefHandle(id);
        _shaders.UseShader(handle);
        _boundShaderId = id;

        uniforms.CopyTo(_boundUniforms.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UseShader(ShaderId id)
    {
        if (_boundShaderId == id) return;

        if (id == default)
        {
            _boundShaderId = default;
            _boundUniforms.AsSpan().Clear();
            _shaders.UnbindShader();
            return;
        }

        var handle = _shaderStore.GetRefHandle(id);
        _shaders.UseShader(handle);
        _boundShaderId = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindMesh(MeshId id)
    {
        if (_boundMeshId == id) return;

        if (id == default)
        {
            _states.UnbindMesh();
            _boundMeshId = default;
            _boundMeshMeta = default;
            return;
        }

        var handle = _meshStore.GetRefAndMeta(id, out _boundMeshMeta);
        _boundMeshId = id;
        _states.BindMesh(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawMesh(int instanceCount = 0)
    {
        Debug.Assert(_boundMeshId > 0);
        var meta = _boundMeshMeta;
        var count = meta.DrawCount;

        switch (meta.Kind)
        {
            case DrawMeshKind.Arrays:
                _states.DrawArrays(meta.Primitive, count);
                break;
            case DrawMeshKind.Elements:
                Debug.Assert(meta.ElementSize != DrawElementSize.Invalid);
                _states.DrawElements(meta.Primitive, meta.ElementSize, count);
                break;
            case DrawMeshKind.ArraysInstanced:
                var drawInstances = instanceCount > 0 ? instanceCount : meta.InstanceCount;
                Debug.Assert(drawInstances > 0);
                _states.DrawInstanced(meta.Primitive, meta.ElementSize, count, drawInstances);
                _drawInstanceCount += drawInstances;
                break;
            case DrawMeshKind.Invalid:
            default:
                GraphicsException.ThrowUnsupportedFeature(meta.Kind.ToString());
                return;
        }

        _drawTriangleCount += count;
        _drawCallCount++;
    }


    // Dont use for now.
    public void SetUniform(int uniform, int value) => _shaders.SetUniform(_boundUniforms![uniform], value);

    public void SetUniform(int uniform, uint value) => _shaders.SetUniform(_boundUniforms![uniform], value);

    public void SetUniform(int uniform, float value) => _shaders.SetUniform(_boundUniforms![uniform], value);

    public void SetUniform(int uniform, Vector2 value) => _shaders.SetUniform(_boundUniforms![uniform], value);

    public void SetUniform(int uniform, Vector3 value) => _shaders.SetUniform(_boundUniforms![uniform], value);

    public void SetUniform(int uniform, in Vector4 value) => _shaders.SetUniform(_boundUniforms![uniform], in value);

    public void SetUniform(int uniform, in Color4 value) => _shaders.SetUniform(_boundUniforms![uniform], in value);

    public void SetUniform(int uniform, in Matrix4x4 value) => _shaders.SetUniform(_boundUniforms![uniform], in value);

    public void SetUniform(int uniform, in Matrix3 value) => _shaders.SetUniform(_boundUniforms![uniform], in value);
}