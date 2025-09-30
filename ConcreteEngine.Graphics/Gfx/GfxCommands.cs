#region

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

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

    private readonly GfxStoreHub _store;
    private readonly GfxResourceRepository _repository;

    //States
    private BlendMode _blendMode = BlendMode.Unset;
    private DepthMode _depthMode = DepthMode.Unset;
    private CullMode _cullMode = CullMode.Unset;

    private GfxPassState _activeState;
    private GfxPassClear _activeClear;

    private readonly TextureId[] _boundTextures;

    private FrameBufferId _boundFboId = default;

    private MeshId _boundMeshId = default;
    private MeshMeta _boundMeshMeta = default;

    private ShaderId _boundShaderId = default;
    private int[]? _boundUniforms = Array.Empty<int>();

    //
    private Bounds2D _activeOutputSize;
    private FrameInfo _frameCtx;
    private int _drawTriangleCount = 0;
    private int _drawCallCount = 0;

    public GfxPassState ActiveState => _activeState;


    internal GfxCommands(GfxContextInternal ctx)
    {
        _driver = ctx.Driver;
        _states = ctx.Driver.States;
        _shaders = ctx.Driver.Shaders;
        _textures = ctx.Driver.Textures;
        _repository = ctx.Repositories;
        _store = ctx.Stores;

        _boundTextures = new TextureId[Configuration.MaxTextureImageUnits];

        SetBlendMode(BlendMode.Alpha);
        SetDepthMode(DepthMode.Lequal);
        SetCullMode(CullMode.BackCcw);
    }


    internal void BeginFrame(in FrameInfo frameCtx)
    {
        _frameCtx = frameCtx;

        _drawCallCount = 0;
        _drawTriangleCount = 0;

        _activeOutputSize = _frameCtx.OutputSize;

        //Clear(Color4.CornflowerBlue, ClearBufferFlag.ColorAndDepth);
        SetDepthMode(DepthMode.Lequal);
    }

    internal void EndFrame(out GpuFrameStats result)
    {
        result = new GpuFrameStats(_drawCallCount, _drawTriangleCount);
        UseShader(default, Array.Empty<int>());
        BindMesh(default);
        BindFramebuffer(default);

        _blendMode = BlendMode.Unset;
        _depthMode = DepthMode.Unset;
        _cullMode = CullMode.Unset;

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
        
        _activeOutputSize = Bounds2D.FromSize(meta.Size);
        /*
*/
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
        var fromHandle = _store.FboStore.GetRef(fromId);
        var srcSize = fromFbo.Size;
        
        if (!_store.FboStore.TryGetRef(toId, out var toHandle, out var toFbo))
        {
            _driver.FrameBuffers.BlitDefault(fromHandle, srcSize, _activeOutputSize.ToSize2D(), false);
            return;
        }

        _driver.FrameBuffers.Blit(fromHandle, toHandle, srcSize, toFbo.Size, linear);
    }


    public void Clear(in GfxPassClear passClear)
    {
        if (passClear.ClearColor is { } clearColor) _states.ClearColor(clearColor);
        if (passClear.ClearBuffer is { } clearBuff) _states.ClearBuffer(clearBuff);
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
    }
    

    public void SetViewport(Size2D viewportSize)
    {
        _activeOutputSize = Bounds2D.FromSize(viewportSize);
        _states.SetViewport(_activeOutputSize);
    }
    
    public void SetViewport(Bounds2D viewport)
    {
        _activeOutputSize = viewport;
        _states.SetViewport(viewport);
    }
    

    public void SetBlendMode(BlendMode blendMode)
    {
        if (_blendMode != BlendMode.Unset && _blendMode == blendMode) return;
        _blendMode = blendMode;
        _states.SetBlendMode(blendMode);
    }

    public void SetDepthMode(DepthMode depthMode)
    {
        if (_depthMode != DepthMode.Unset && _depthMode == depthMode) return;
        _depthMode = depthMode;
        _states.SetDepthMode(depthMode);
    }

    public void SetCullMode(CullMode cullMode)
    {
        if (_cullMode != CullMode.Unset && _cullMode == cullMode) return;
        _cullMode = cullMode;
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

        _states.BindFrameBuffer(_store.FboStore.GetRef(id));
        _boundFboId = id;
    }


    public void BindTexture(TextureId texture, int slot)
    {
        Debug.Assert(slot >= 0 && slot <= Configuration.MaxTextureImageUnits);

        if (_boundTextures[slot] == texture) return;
        _boundTextures[slot] = texture;
        if (texture.Value == 0)
        {
            _states.UnbindTextureSlot(slot);
            return;
        }

        var refHandle = _store.TextureStore.GetRef(texture);
        _states.BindTexture(refHandle, slot);
    }

    public void BindMesh(MeshId id)
    {
        if (_boundMeshId == id) return;

        if (id == default)
        {
            _driver.States.UnbindMesh();
            _boundMeshId = default;
            _boundMeshMeta = default;
            return;
        }

        var meshRef = _store.MeshStore.GetRefAndMeta(id, out _boundMeshMeta);
        _driver.States.BindMesh(meshRef);
        _boundMeshId = id;
    }


    public void DrawBoundMesh(MeshId id, int drawCount)
    {
        //_boundMeshMeta
        ref readonly var meta = ref _store.MeshStore.GetMeta(_boundMeshId);
        var count = drawCount > 0 ? drawCount : meta.DrawCount;

        switch (meta.DrawKind)
        {
            case MeshDrawKind.Arrays:
                DrawArrays(meta.Primitive, count);
                break;
            case MeshDrawKind.Elements:
                DrawElements(meta.Primitive, meta.ElementSize, count);
                break;
            default:
                GraphicsException.ThrowUnsupportedFeature(nameof(meta.DrawKind));
                break;
        }
    }

    private void DrawArrays(DrawPrimitive primitive, int drawCount)
    {
        Debug.Assert(drawCount != 0);
        _driver.States.DrawArrays(primitive, drawCount);
        _drawTriangleCount += drawCount;
        _drawCallCount++;
    }

    private void DrawElements(DrawPrimitive primitive, DrawElementSize elementSize, int drawCount)
    {
        Debug.Assert(drawCount != 0);
        Debug.Assert(elementSize != DrawElementSize.Invalid);

        _driver.States.DrawElements(primitive, elementSize, drawCount);
        _drawTriangleCount += drawCount;
        _drawCallCount++;
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
        var handle = _store.ShaderStore.GetRef(id);
        _shaders.UseShader(handle);
        _boundShaderId = id;
        _boundUniforms = uniformLocations;
    }
    


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, int value) 
        => _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, uint value) 
        => _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, float value) 
        => _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector2 value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector3 value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector4 value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, in Matrix4x4 value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], in value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, in Matrix3 value) =>
        _shaders.SetUniform(_boundUniforms![(int)uniform], in value);
}