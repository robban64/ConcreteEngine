#region

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxCommands
{
    public GraphicsConfiguration Configuration => _driver.Configuration;
    public DeviceCapabilities Capabilities => _driver.Capabilities;

    private readonly IGraphicsDriver _driver;
    private readonly GlStates _states;
    private readonly GlShaders _shaders;
    private readonly GlTextures _textures;
    private readonly GlFrameBuffers _frameBuffers;

    private readonly GfxStoreHub _store;

    //States
    private GfxPassState _activeState;
    private GfxPassStateFunc _stateFunc;
    private GfxPassClear _activeClear;

    private readonly TextureId[] _boundTextures;

    private FrameBufferId _boundFboId = default;

    private MeshId _boundMeshId = default;
    private MeshMeta _boundMeshMeta = default;

    private ShaderId _boundShaderId = default;
    private int[]? _boundUniforms = Array.Empty<int>();

    //
    private Size2D _activeOutputSize;
    private GfxFrameInfo _frameCtx;
    private int _drawTriangleCount = 0;
    private int _drawCallCount = 0;

    public GfxPassState ActiveState => _activeState;


    internal GfxCommands(GfxContextInternal ctx)
    {
        _driver = ctx.Driver;
        _states = ctx.Driver.States;
        _shaders = ctx.Driver.Shaders;
        _textures = ctx.Driver.Textures;
        _frameBuffers = ctx.Driver.FrameBuffers;
        _store = ctx.Stores;

        _boundTextures = new TextureId[Configuration.TextureSlots];

        SetBlendMode(BlendMode.Alpha);
        SetDepthMode(DepthMode.Lequal);
        SetCullMode(CullMode.BackCcw);
    }


    internal void BeginFrame(in GfxFrameInfo frameCtx)
    {
        _frameCtx = frameCtx;

        _drawCallCount = 0;
        _drawTriangleCount = 0;

        _activeOutputSize = _frameCtx.OutputSize;
    }

    internal void EndFrame(out GfxFrameResult result)
    {
        result = new GfxFrameResult(_drawCallCount, _drawTriangleCount);
        UseShader(default, Array.Empty<int>());
        BindMesh(default);
        BindFramebuffer(default);

        //_stateFunc = new GfxPassStateFunc(BlendMode.Unset, CullMode.Unset, DepthMode.Unset);

        _driver.EndFrame();
    }

    public void BeginScreenPass(in GfxPassClear passClear, in GfxPassState states)
    {
        BindFramebuffer(default);
        SetViewport(_activeOutputSize);

        ApplyState(in states);
        Clear(in passClear);

        _activeOutputSize = _frameCtx.OutputSize;
    }

    public void BeginRenderPass(FrameBufferId fboId, in GfxPassClear passClear, in GfxPassState states)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fboId.Value, nameof(fboId));
        if (_boundFboId == fboId) GraphicsException.ThrowInvalidState($"FBO is {fboId} already bound.");

        ref readonly var meta = ref _store.FboStore.GetMeta(fboId);

        BindFramebuffer(fboId);
        SetViewport(meta.Size);
        ApplyState(in states);
        Clear(in passClear);

        if (meta.Attachments.DepthTextureId != default)
        {
            _frameBuffers.SetDrawReadBuffer(_store.FboStore.GetRefHandle(fboId), false);
        }

        _activeOutputSize = meta.Size;
    }

    public void EndRenderPass()
    {
        if (_boundFboId == default) GraphicsException.ResourceNotBound<GlFboHandle>(nameof(_boundFboId));

        BindFramebuffer(default);
        _activeOutputSize = _frameCtx.OutputSize;

        SetViewport(_activeOutputSize);
    }


    public void BlitFramebuffer(FrameBufferId fromId, FrameBufferId toId, bool linear)
    {
        Debug.Assert(fromId != default);
        Debug.Assert(fromId != toId, "READ and DRAW FBO must differ for resolve.");

        ref readonly var fromFbo = ref _store.FboStore.GetMeta(fromId);
        var fromHandle = _store.FboStore.GetRefHandle(fromId);
        var srcSize = fromFbo.Size;

        if (!_store.FboStore.TryGetRef(toId, out var toHandle, out var toFbo))
        {
            _frameBuffers.BlitDefault(fromHandle, srcSize, _activeOutputSize, false);
            return;
        }

        _frameBuffers.Blit(fromHandle, toHandle, srcSize, toFbo.Size, linear);
    }


    public void Clear(in GfxPassClear passClear)
    {
        if (passClear.ClearBuffer is ClearBufferFlag.Color or ClearBufferFlag.ColorAndDepth)
            _states.ClearColor(passClear.ClearColor);

        if (passClear.ClearBuffer is not ClearBufferFlag.None)
            _states.ClearBuffer(passClear.ClearBuffer);
    }

    public void ApplyState(in GfxPassState cmdState)
    {
        _activeState = cmdState;
        if (cmdState.Scissor is { } scissor) _states.ToggleScissorTest(scissor);
        if (cmdState.Cull is { } cull) _states.ToggleCullFace(cull);
        if (cmdState.DepthTest is { } depthTest) _states.ToggleDepthTest(depthTest);
        if (cmdState.DepthWrite is { } depthWrite) _states.ToggleDepthMask(depthWrite);
        if (cmdState.Blend is { } blend) _states.ToggleBlendState(blend);
        if (cmdState.FramebufferSrgb is { } srgb) _states.ToggleFrameBufferSrgb(srgb);
        if (cmdState.ColorMask is { } colorMask) _states.ColorMask(colorMask);
        if (cmdState.PolygonOffset is { } polygonOffset) _states.TogglePolygonOffset(polygonOffset);
    }

    public void ApplyStateFunctions(GfxPassStateFunc cmdFunc)
    {
        _stateFunc = cmdFunc;
        SetBlendMode(cmdFunc.Blend);
        SetCullMode(cmdFunc.Cull);
        SetDepthMode(cmdFunc.Depth);
        SetPolygonOffset(cmdFunc.PolygonOffset);
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


    public void BindFramebuffer(FrameBufferId id)
    {
        if (_boundFboId == id) return;
        if (id == default)
        {
            _states.UnbindFrameBuffer();
            _boundFboId = default;
            return;
        }

        _states.BindFrameBuffer(_store.FboStore.GetRefHandle(id));
        _boundFboId = id;
    }


    public void BindTexture(TextureId texture, int slot)
    {
        Debug.Assert(slot >= 0 && slot <= Configuration.TextureSlots);

        if (_boundTextures[slot] == texture) return;
        _boundTextures[slot] = texture;
        if (texture.Value == 0)
        {
            _states.UnbindTextureSlot(slot);
            return;
        }

        var refHandle = _store.TextureStore.GetRefHandle(texture);
        _states.BindTexture(refHandle, slot);
    }

    public void UseShader(ShaderId id, int[] uniformLocations)
    {
        if (_boundShaderId == id) return;

        if (id == default)
        {
            _boundShaderId = default;
            _boundUniforms = null;
            _shaders.UnbindShader();
            return;
        }

        var handle = _store.ShaderStore.GetRefHandle(id);
        _shaders.UseShader(handle);
        _boundShaderId = id;
        _boundUniforms = uniformLocations;
    }

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

        var meshRef = _store.MeshStore.GetRefAndMeta(id, out _boundMeshMeta);
        _boundMeshId = id;
        _states.BindMesh(meshRef);
    }


    public void DrawBoundMesh(MeshId id, int drawCount)
    {
        Debug.Assert(_boundMeshId > 0);

        var meta = _boundMeshMeta;
        var count = drawCount > 0 ? drawCount : meta.DrawCount;

        switch (meta.Kind)
        {
            case DrawMeshKind.Arrays:
                _states.DrawArrays(meta.Primitive, count);
                break;
            case DrawMeshKind.Elements:
                Debug.Assert(meta.ElementSize != DrawElementSize.Invalid);
                _states.DrawElements(meta.Primitive, meta.ElementSize, count);
                break;
            default:
                GraphicsException.ThrowUnsupportedFeature(nameof(meta.Kind));
                return;
        }

        _drawTriangleCount += count;
        _drawCallCount++;
    }


    public void SetUniform(ShaderUniform uniform, int value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    public void SetUniform(ShaderUniform uniform, uint value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    public void SetUniform(ShaderUniform uniform, float value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    public void SetUniform(ShaderUniform uniform, Vector2 value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    public void SetUniform(ShaderUniform uniform, Vector3 value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    public void SetUniform(ShaderUniform uniform, Vector4 value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    public void SetUniform(ShaderUniform uniform, in Matrix4x4 value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], in value);

    public void SetUniform(ShaderUniform uniform, in Matrix3 value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], in value);
}