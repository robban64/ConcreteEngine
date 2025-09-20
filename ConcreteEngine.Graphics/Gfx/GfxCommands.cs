using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxCommands 
{
    public GraphicsConfiguration Configuration => _driver.Configuration;
    public DeviceCapabilities Capabilities => _driver.Capabilities;

    private readonly IGraphicsDriver _driver;
    private readonly GlStates _states;
    private readonly GlShaders _shaders;
    private readonly GlTextures _textures;

    private readonly FrontendStoreHub _store;
    private readonly GfxResourceRepository _repository;

    //States
    private BlendMode _blendMode = BlendMode.Unset;
    private DepthMode _depthMode = DepthMode.Unset;
    private CullMode _cullMode = CullMode.Unset;

    //TODO remove
    private FrameBufferId _boundFboId = default;
    private FrameBufferId _boundReadFboId = default;
    private GfxHandle _currDrawFboHandle = default; // 0 = screen
    private GfxHandle _currReadFboHandle = default; // 0 = screen


    private ShaderId _boundShaderId = default;
    private readonly TextureId[] _boundTextures;

    private ShaderLayout? _boundUniforms;

    private MeshId _boundMeshId = default;
    private MeshMeta _boundMeshMeta = default;

    //
    private Vector2D<int> _activeOutputSize;
    private FrameInfo _frameCtx;
    private int _drawTriangleCount = 0;
    private int _drawCallCount = 0;

    internal GfxCommands(GfxContextInternal ctx)
    {
        _driver = ctx.Driver;
        _states = ctx.Driver.States;
        _shaders = ctx.Driver.Shaders;
        _textures = ctx.Driver.Textures;
        _repository = ctx.Repositories;
        _store = ctx.Stores;

        _boundTextures = new TextureId[Configuration.MaxTextureImageUnits];
    }

    public void BeginFrame(in FrameInfo frameCtx)
    {
        _frameCtx = frameCtx;

        _drawCallCount = 0;
        _drawTriangleCount = 0;

        _activeOutputSize = _frameCtx.OutputSize;

        _states.SetBlendMode(BlendMode.None);
        _states.SetDepthMode(DepthMode.WriteLequal);
        _states.Clear(Color4.CornflowerBlue, ClearBufferFlag.ColorAndDepth);
    }

    public void EndFrame(out GpuFrameStats result)
    {
        result = new GpuFrameStats(_drawCallCount, _drawTriangleCount);
        // unbind context
        UseShader(default);

        _blendMode = BlendMode.Unset;
        _depthMode = DepthMode.Unset;
        _cullMode = CullMode.Unset;

        for (int i = 0; i < _boundTextures.Length; i++)
        {
            BindTexture(default, i);
        }

        _driver.ValidateEndFrame();
    }

    public void BeginScreenPass(Color4? clear = null, ClearBufferFlag? flags = null)
    {
        if (_boundFboId != default) GraphicsException.ThrowInvalidState("Cannot begin screen pass while FBO is bound.");

        _currDrawFboHandle = default;
        _currReadFboHandle = default;

        _boundReadFboId = default;
        _activeOutputSize = _frameCtx.OutputSize;

        SetViewport(_activeOutputSize);

        if (clear.HasValue && flags.HasValue) Clear(clear.Value, flags.Value);
    }

    public void BeginRenderPass(in FrameBufferId fboId, Color4? clear, ClearBufferFlag? flags)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fboId.Value, nameof(fboId));
        if (_boundFboId == fboId) GraphicsException.ThrowInvalidState($"FBO is {fboId} already bound.");

        ref readonly var meta = ref _store.FboStore.GetMeta(fboId);
        ref readonly var handle = ref _store.FboStore.GetHandle(fboId);

        _boundReadFboId = fboId;

        SetViewport(meta.Size);
        if (clear.HasValue && flags.HasValue) Clear(clear.Value, flags.Value);
        SetDepthMode(DepthMode.WriteLequal);
        SetCullMode(CullMode.BackCcw);

        _currDrawFboHandle = handle;
        _currReadFboHandle = handle;

        _activeOutputSize = meta.Size;
        Debug.Assert(_currDrawFboHandle != default && _currDrawFboHandle == _currReadFboHandle);
    }

    public void EndRenderPass()
    {
        if (_boundFboId == default) GraphicsException.ResourceNotBound<GlFboHandle>(nameof(_boundFboId));

        _currDrawFboHandle = default;
        _currReadFboHandle = default;

        _boundReadFboId = default;

        _activeOutputSize = _frameCtx.OutputSize;

        SetViewport(_activeOutputSize);
    }


    public void BlitFramebuffer(in FrameBufferId fromId, in FrameBufferId toId = default, bool linear = true)
    {
        Debug.Assert(fromId != default);
        Debug.Assert(fromId != toId, "READ and DRAW FBO must differ for resolve.");

        ref readonly var fromFbo = ref _store.FboStore.GetMeta(fromId);
        var fromHandle = _store.FboStore.GetHandle(fromId);
        var srcSize = fromFbo.Size;

        if (!_store.FboStore.TryGet(toId, out var toHandle, out var toFbo))
        {
            _driver.FrameBuffers.BlitDefault(in fromHandle, srcSize, _activeOutputSize, linear);
            return;
        }

        _driver.FrameBuffers.Blit(in fromHandle, in toHandle, srcSize, toFbo.Size, linear);

        // Legacy - SetViewport(prevViewport);
    }


    public void Clear(Color4 color, ClearBufferFlag flags) => _states.Clear(color, flags);

    public void SetViewport(in Vector2D<int> viewport)
    {
        if (_activeOutputSize == viewport) return;

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
        if (_depthMode != DepthMode.Disabled && _depthMode == depthMode) return;
        _depthMode = depthMode;
        _states.SetDepthMode(depthMode);
    }

    public void SetCullMode(CullMode cullMode)
    {
        if (_cullMode != CullMode.None && _cullMode == cullMode) return;
        _cullMode = cullMode;
        _states.SetCullMode(cullMode);
    }

    public void UseShader(ShaderId id)
    {
        if (_boundShaderId == id) return;

        if (id == default)
        {
            _boundShaderId = default;
            _boundUniforms = null;
            _shaders.UseShader(default);
            return;
        }

        var handle = _store.ShaderStore.GetHandle(id);
        var uniformTable = _repository.ShaderRepository.GetShaderLayout(id);

        _shaders.UseShader(handle);
        _boundShaderId = id;
        _boundUniforms = uniformTable;
    }


    public void BindTexture(TextureId texture, int slot)
    {
        if (slot >= Configuration.MaxTextureImageUnits)
            GraphicsException.ThrowCapabilityExceeded<TextureId>("TexCoords slot", (int)slot,
                Configuration.MaxTextureImageUnits);

        if (_boundTextures[slot] == texture) return;
        if (texture == default)
        {
            _textures.BindTexture(default, (uint)slot);
            _boundTextures[slot] = default;
            return;
        }


        _boundTextures[slot] = texture;
        ref readonly var handle = ref _store.TextureStore.GetHandle(texture);
        _textures.BindTexture(handle, (uint)slot);
    }

    private void BindMesh(MeshId id)
    {
        if (_boundMeshId == id) return;

        if (id == default)
        {
            _driver.States.BindMesh(default);
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
        Debug.Assert(drawCount != 0, "DrawArrays called with drawCount = 0");
        _driver.States.DrawArrays(primitive, (uint)drawCount);
        _drawTriangleCount += drawCount;
        _drawCallCount++;
    }

    private void DrawElements(DrawPrimitive primitive, DrawElementSize elementSize, int drawCount)
    {
        Debug.Assert(drawCount != 0, "DrawElements called with drawCount = 0");
        Debug.Assert(elementSize != DrawElementSize.Invalid);

        _driver.States.DrawElements(primitive, elementSize, (uint)drawCount);
        _drawTriangleCount += drawCount;
        _drawCallCount++;
    }
    


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, int value) 
        => _shaders.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, uint value) 
        => _shaders.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, float value) 
        => _shaders.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector2 value) =>
        _shaders.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector3 value) =>
        _shaders.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector4 value) =>
        _shaders.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, in Matrix4x4 value) =>
        _shaders.SetUniform(_boundUniforms![uniform], in value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, in Matrix3 value) =>
        _shaders.SetUniform(_boundUniforms![uniform], in value);
}