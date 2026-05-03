using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
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
    private static Size2D _outputSize;
    private static Size2D _activeOutputSize;
    private static readonly TextureId[] BoundTextures = new TextureId[GfxLimits.TextureSlots];

    private readonly GlStates _states;
    private readonly GlShaders _shaders;
    private readonly GlFrameBuffers _frameBuffers;
    private readonly GlDraw _glDraw;

    private readonly FboStore _fboStore;
    private readonly TextureStore _textureStore;
    private readonly MeshStore _meshStore;
    private readonly ShaderStore _shaderStore;

    //States
    private MeshId _boundMeshId;
    private MeshMeta _boundMeshMeta;

    private FrameBufferId _boundFboId;
    private ShaderId _boundShaderId;

    //
    private GfxStateFlags _activeFlags;
    private GfxPassFunctions _passFunctions;


    internal GfxCommands(GfxContextInternal ctx)
    {
        _states = ctx.Driver.States;
        _shaders = ctx.Driver.Shaders;
        _frameBuffers = ctx.Driver.FrameBuffers;

        _fboStore = ctx.Resources.GfxStoreHub.FboStore;
        _textureStore = ctx.Resources.GfxStoreHub.TextureStore;
        _meshStore = ctx.Resources.GfxStoreHub.MeshStore;
        _shaderStore = ctx.Resources.GfxStoreHub.ShaderStore;

        _glDraw = GlDraw.Instance;

        SetBlendMode(BlendMode.Alpha);
        SetDepthMode(DepthMode.Lequal);
        SetCullMode(CullMode.BackCcw);
    }

    internal void BeginFrame(GfxFrameArgs args)
    {
        _glDraw.FrameMeta = default;
        _outputSize = args.OutputSize;
        _activeOutputSize = args.OutputSize;
    }

    internal void EndFrame(out RenderFrameMeta result)
    {
        result = _glDraw.FrameMeta;
        UseShader(default);
        BindMesh(default);
        BindFramebuffer(default);

        //_stateFunc = new GfxPassStateFunc(BlendMode.Unset, CullMode.Unset, DepthMode.Unset);
        Array.Clear(BoundTextures);
    }

    public void BeginScreenPass(GfxPassClear passClear, GfxPassState states)
    {
        BindFramebuffer(default);
        SetViewport(_activeOutputSize);
        ApplyState(states);

        Clear(passClear);

        _activeOutputSize = _outputSize;
    }


    public void BeginRenderPass(FrameBufferId fboId, GfxPassClear passClear, GfxPassState states)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fboId.Value, nameof(fboId));
        if (_boundFboId == fboId) GraphicsException.ThrowInvalidState($"FBO is {fboId} already bound.");

        var size = _fboStore.GetMeta(fboId).Size;

        BindFramebuffer(fboId);
        SetViewport(size);
        ApplyState(states);
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
            case ClearBufferFlag.Color: _states.ClearColor(passClear.ClearColor); break;
            case ClearBufferFlag.Depth: _states.ClearBuffer(passClear.ClearBuffer); break;
            case ClearBufferFlag.ColorAndDepth:
                _states.ClearColor(passClear.ClearColor);
                _states.ClearBuffer(passClear.ClearBuffer);
                break;
        }
    }


    public void ApplyState(GfxPassState state)
    {
        var d = state.Defined;
        if (d == 0) return;
        var e = state.Enabled;

        var states = _states;
        if ((d & GfxStateFlags.Scissor) != 0)
            states.ToggleScissorTest((e & GfxStateFlags.Scissor) != 0);
        if ((d & GfxStateFlags.Cull) != 0)
            states.ToggleCullFace((e & GfxStateFlags.Cull) != 0);
        if ((d & GfxStateFlags.DepthTest) != 0)
            states.ToggleDepthTest((e & GfxStateFlags.DepthTest) != 0);
        if ((d & GfxStateFlags.DepthWrite) != 0)
            states.ToggleDepthMask((e & GfxStateFlags.DepthWrite) != 0);
        if ((d & GfxStateFlags.Blend) != 0)
            states.ToggleBlendState((e & GfxStateFlags.Blend) != 0);
        if ((d & GfxStateFlags.FramebufferSrgb) != 0)
            states.ToggleFrameBufferSrgb((e & GfxStateFlags.FramebufferSrgb) != 0);
        if ((d & GfxStateFlags.ColorMask) != 0)
            states.ColorMask((e & GfxStateFlags.ColorMask) != 0);
        if ((d & GfxStateFlags.PolygonOffset) != 0)
            states.TogglePolygonOffset((e & GfxStateFlags.PolygonOffset) != 0);
        if ((d & GfxStateFlags.SampleAlphaCoverage) != 0)
            states.ToggleSampleAlphaCoverage((e & GfxStateFlags.SampleAlphaCoverage) != 0);

        _activeFlags = GfxPassState.Merge(_activeFlags, state);
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
        _states.SetViewport(_activeOutputSize.ToBounds2D());
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPolygonOffset(PolygonOffsetLevel polygon)
    {
        if (_passFunctions.PolygonOffset != PolygonOffsetLevel.Unset && _passFunctions.PolygonOffset == polygon) return;
        var (factor, units) = polygon.ToFactorUnits();
        _passFunctions.PolygonOffset = polygon;
        _states.SetPolygonOffset(factor, units);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlendMode(BlendMode blendMode)
    {
        if (_passFunctions.Blend != BlendMode.Unset && _passFunctions.Blend == blendMode) return;
        _passFunctions.Blend = blendMode;
        _states.SetBlendMode(blendMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDepthMode(DepthMode depthMode)
    {
        if (_passFunctions.Depth != DepthMode.Unset && _passFunctions.Depth == depthMode) return;
        _passFunctions.Depth = depthMode;
        _states.SetDepthMode(depthMode);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCullMode(CullMode cullMode)
    {
        if (_passFunctions.Cull != CullMode.Unset && _passFunctions.Cull == cullMode) return;
        _passFunctions.Cull = cullMode;
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

        _states.BindFrameBuffer(_fboStore.GetHandle(id));
        _boundFboId = id;
    }

    public void BindTexture(TextureId texture, int slot)
    {
        Debug.Assert(slot >= 0 && slot <= GfxLimits.TextureSlots);
        ref var boundTexture = ref BoundTextures[slot];
        if (boundTexture == texture) return;
        boundTexture = texture;

        if (boundTexture == 0)
        {
            _states.UnbindTextureSlot(slot);
            return;
        }

        var refHandle = _textureStore.GetHandle(boundTexture);
        _states.BindTexture(refHandle, slot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindAllTextures()
    {
        Array.Clear(BoundTextures);
        _states.UnbindAllTextures();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UseShader(ShaderId id)
    {
        if (_boundShaderId == id) return;

        if (id == default)
        {
            _boundShaderId = default;
            _shaders.UnbindShader();
            return;
        }

        var handle = _shaderStore.GetHandle(id);
        _shaders.UseShader(handle);
        _boundShaderId = id;
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

        var handle = _meshStore.GetHandleAndMeta(id, out _boundMeshMeta);

        _boundMeshId = id;
        _states.BindMesh(handle);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindAndDrawMesh(MeshId id, uint instanceCount = 0)
    {
        ref var meta = ref _boundMeshMeta;
        if (_boundMeshId != id)
        {
            var handle = _meshStore.GetHandleAndMeta(id, out _boundMeshMeta);
            _states.BindMesh(handle);
            _boundMeshId = id;
        }

        var instances = uint.Max(meta.InstanceCount, instanceCount);
        _glDraw.DrawMesh(meta.Kind, meta.Primitive, meta.ElementSize, meta.DrawCount, instances);
    }

    /*
     *     [MethodImpl(MethodImplOptions.AggressiveInlining)]
       public void DrawMeshOld(uint instanceCount = 0)
       {
           Debug.Assert(_boundMeshId > 0);
           ref readonly var meta = ref _boundMeshMeta;
           switch (meta.Kind)
           {
               case DrawMeshKind.Arrays:
                   _states.DrawArrays(meta.Primitive, meta.DrawCount);
                   break;
               case DrawMeshKind.Elements:
                   Debug.Assert(meta.ElementSize != DrawElementSize.None);
                   _states.DrawElements(meta.Primitive, meta.ElementSize, meta.DrawCount);
                   break;
               case DrawMeshKind.ArraysInstanced:
                   var drawInstances = uint.Max(instanceCount, meta.InstanceCount);
                   _states.DrawInstanced(meta.Primitive, meta.ElementSize, meta.DrawCount, drawInstances);
                   _frameMeta.Instances += drawInstances;
                   break;
               case DrawMeshKind.Invalid:
               default:
                   GraphicsException.ThrowUnsupportedMesh(meta.Kind);
                   return;
           }

           _frameMeta.AddDrawCall(meta.DrawCount);
       }
     */
}