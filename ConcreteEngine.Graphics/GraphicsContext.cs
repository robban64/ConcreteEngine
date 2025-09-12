using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;

public interface IGraphicsContext
{
    GraphicsConfiguration Configuration { get; }
    DeviceCapabilities Capabilities { get; }

    //
    void BeginFrame(in FrameInfo frameCtx);
    void EndFrame(out GpuFrameStats result);

    void Clear(Color4 color, ClearBufferFlag flags);
    void SetBlendMode(BlendMode blendMode);
    void SetDepthMode(DepthMode depthMode);
    void SetCullMode(CullMode cullMode);
    void SetViewport(in Vector2D<int> viewport);


    void BeginScreenPass(Color4? clear = null, ClearBufferFlag? flags = null);
    void BeginRenderPass(in FrameBufferId fboId, Color4? clear, ClearBufferFlag? flags);
    void EndRenderPass();
    void BlitFramebuffer(in FrameBufferId fromId, in FrameBufferId toId = default, bool linear = true);

    void BindTexture(TextureId resourceId, uint slot);
    void BindMesh(MeshId id);
    void BindVertexBuffer(VertexBufferId id);
    void BindIndexBuffer(IndexBufferId id);
    void BindUniformBuffer(UniformGpuSlot slot);
    void BindFramebuffer(FrameBufferId id);


    void SetVertexAttribute(ReadOnlySpan<VertexAttributeDescriptor> attributes);
    void SetVertexBuffer<T>(ReadOnlySpan<T> data, BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;
    void SetIndexBuffer<T>(ReadOnlySpan<T> data, BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;

    void UploadVertexBuffer<T>(VertexBufferId vbo, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged;
    void UploadIndexBuffer<T>(IndexBufferId ibo, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged;

    void SetUniformBufferSize(UniformGpuSlot slot, nuint capacityBytes);
    void UploadUniformGpuData<T>(UniformGpuSlot slot, in T data, nuint offset = 0) where T : unmanaged, IUniformGpuData;
    void BindUniformBufferRange(UniformGpuSlot slot, nuint offset, nuint size);

    void DrawArrays(DrawPrimitive primitive, uint drawCount);
    void DrawElements(DrawPrimitive primitive, uint drawCount, DrawElementType elementType);

    void UseShader(ShaderId id);
    void SetUniform(ShaderUniform uniform, int value);
    void SetUniform(ShaderUniform uniform, uint value);
    void SetUniform(ShaderUniform uniform, float value);
    void SetUniform(ShaderUniform uniform, Vector2 value);
    void SetUniform(ShaderUniform uniform, Vector3 value);
    void SetUniform(ShaderUniform uniform, Vector4 value);
    void SetUniform(ShaderUniform uniform, in Matrix4x4 value);
    void SetUniform(ShaderUniform uniform, in Matrix3 value);
    
   }
internal interface IGpuProgramContext;
internal interface IGpuBufferContext;
internal interface IGpuMeshContext;
internal interface IGpuTextureContext;
internal interface IGpuFramebufferContext;
internal interface IGpuStateContext;


internal sealed class GraphicsContext : IGraphicsContext
{
    public GraphicsConfiguration Configuration { get; }
    public DeviceCapabilities Capabilities { get; }

    private readonly IGraphicsDriver _driver;
    private readonly ResourceManager _store;

    private readonly ShaderRegistry _shaderRegistry;
    private readonly MeshRegistry _meshRegistry;


    //States
    private BlendMode _blendMode = BlendMode.Alpha;
    private DepthMode _depthMode = DepthMode.WriteLequal;
    private CullMode _cullMode = CullMode.BackCcw;

    private FrameBufferId _boundFboId = default;
    private FrameBufferId _boundReadFboId = default;
    private FrameBufferId _currDrawFboId = default; // 0 = screen
    private FrameBufferId _currReadFboId = default; // 0 = screen


    private ShaderId _boundShaderId = default;
    private VertexBufferId _boundVertexBufferId = default;
    private IndexBufferId _boundIndexBufferId = default;
    private UniformBufferId _boundUniformBufferId = default;
    private MeshId _boundVaoId = default;
    private readonly TextureId[] _boundTextures;

    private ShaderLayout? _boundUniforms;

    //
    private Vector2D<int> _activeOutputSize;
    private FrameInfo _frameCtx;
    private int _drawTriangleCount = 0;
    private int _drawCallCount = 0;

    public GraphicsContext()
    {
        _store = new ResourceManager();
        
        _boundTextures = new TextureId[Capabilities.MaxTextureImageUnits];

    }

    public void BeginFrame(in FrameInfo frameCtx)
    {
        _frameCtx = frameCtx;

        _drawCallCount = 0;
        _drawTriangleCount = 0;

        _activeOutputSize = _frameCtx.OutputSize;

        SetBlendMode(BlendMode.None);
        SetDepthMode(DepthMode.WriteLequal);
        Clear(Colors.CornflowerBlue, ClearBufferFlag.ColorAndDepth);
    }

    public void EndFrame(out GpuFrameStats result)
    {
        result = new GpuFrameStats(_drawCallCount, _drawTriangleCount);

        // unbind context
        BindMesh(default);
        BindVertexBuffer(default);
        BindIndexBuffer(default);
        BindFramebuffer(default);
        BindUniformBuffer(default);
        UseShader(default);
        for (uint i = 0; i < _boundTextures.Length; i++)
        {
            BindTexture(default, i);
        }
    }

    public void Clear(Color4 color, ClearBufferFlag flags) => _driver.Clear(color, flags);

    public void SetViewport(in Vector2D<int> viewport)
    {
        _activeOutputSize = viewport;
        _driver.SetViewport(viewport);
    }

    public void BeginScreenPass(Color4? clear = null, ClearBufferFlag? flags = null)
    {
        if (clear.HasValue && flags.HasValue) _driver.Clear(clear.Value, flags.Value);
    }

    public void BeginRenderPass(in FrameBufferId fboId, Color4? clear, ClearBufferFlag? flags)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fboId.Id, nameof(fboId));
        if (_boundFboId == fboId) GraphicsException.ThrowInvalidState($"FBO is {fboId} already bound.");

        ref readonly var meta = ref _store.FboStore.GetMeta(fboId);
        var handle = _store.FboStore.GetHandle(fboId);
        _driver.BindFramebuffer(handle);
        SetViewport(meta.Size);
        if (clear.HasValue && flags.HasValue) Clear(clear.Value, flags.Value);
        SetDepthMode(DepthMode.WriteLequal);
        SetCullMode(CullMode.BackCcw);

        _currDrawFboId = fboId;
        _currReadFboId = fboId;

        _boundFboId = fboId;
        _boundReadFboId = fboId;
        _activeOutputSize = meta.Size;
    }

    public void EndRenderPass()
    {
        if (_boundFboId == default) GraphicsException.ResourceNotBound<GlFboHandle>(nameof(_boundFboId));
        
        _currDrawFboId = default;
        _currDrawFboId = default;
        
        _boundFboId = default;
        _boundReadFboId = default;
        
        _activeOutputSize = _frameCtx.OutputSize;

        _driver.BindFramebuffer(default);
        SetViewport(_activeOutputSize);
    }


    public void BlitFramebuffer(in FrameBufferId fromId, in FrameBufferId toId = default, bool linear = true)
    {
        ref readonly var fromFbo = ref _store.FboStore.GetMeta(fromId);
        var fromHandle = _store.FboStore.GetHandle(fromId);

        var srcSize = fromFbo.Size;

        GfxHandle toHandle = default;
        var dstSize = _activeOutputSize;
        if (toId != default)
        {
            ref readonly var toFbo = ref _store.FboStore.GetMeta(toId);
            toHandle = _store.FboStore.GetHandle(toId);
            dstSize = toFbo.Size;
        }

        Debug.Assert(toHandle != fromHandle, "READ and DRAW FBO must differ for resolve.");

        var prevViewport = _activeOutputSize;
        var prevReadFbo = _store.FboStore.GetHandle(_currReadFboId);
        var prevDrawFbo = _store.FboStore.GetHandle(_currDrawFboId);

        var filter = !fromFbo.Msaa && linear;
        _driver.BindFrameBufferReadDraw(fromHandle, toHandle);
        _driver.Blit(srcSize, dstSize, filter);
        _driver.BindFrameBufferReadDraw(prevReadFbo, prevDrawFbo);
        SetViewport(prevViewport);
    }

    public void SetBlendMode(BlendMode blendMode)
    {
        if (_blendMode != BlendMode.Unset && _blendMode == blendMode) return;
        _blendMode = blendMode;

        _driver.SetBlendMode(blendMode);
    }

    public void SetDepthMode(DepthMode depthMode)
    {
        if (_depthMode != DepthMode.Disabled && _depthMode == depthMode) return;
        _depthMode = depthMode;
        _driver.SetDepthMode(depthMode);
    }

    public void SetCullMode(CullMode cullMode)
    {
        if (_cullMode != CullMode.None && _cullMode == cullMode) return;
        _cullMode = cullMode;
        _driver.SetCullMode(cullMode);
    }


    public void UseShader(ShaderId id)
    {
        if (_boundShaderId == id) return;

        if (id == default)
        {
            _boundShaderId = default;
            _boundUniforms = null;
            _driver.UseShader(default);
            return;
        }

        var handle = _store.ShaderStore.GetHandle(id);
        var uniformTable = _shaderRegistry.GetShaderLayout(id);

        _driver.UseShader(handle);
        _boundShaderId = id;
        _boundUniforms = uniformTable;
    }


    public void BindUniformBuffer(UniformGpuSlot slot)
    {
        var ubo = _shaderRegistry.GetUboId(slot);
        var handle = _store.UboStore.GetHandle(ubo);
        _driver.BindUniformBuffer(handle);
    }

    public void BindFramebuffer(FrameBufferId id)
    {
        if (_boundFboId == id) return;
        if (id == default)
        {
            _driver.BindFramebuffer(default);
            _boundFboId = default;
            return;
        }

        var handle = _store.FboStore.GetHandle(id);
        _driver.BindFramebuffer(handle);
        _boundFboId = id;
    }

    public void BindTexture(TextureId texture, uint slot)
    {
        _boundTextures[slot] = texture;
        var handle = _store.TextureStore.GetHandle(texture);
        _driver.BindTextureUnit(handle, slot);
    }

    public void BindMesh(MeshId id)
    {
        if (_boundVaoId == id) return;

        if (id == default)
        {
            _driver.BindVertexArray(default);
            _boundVaoId = default;
            return;
        }

        var handle = _store.MeshStore.GetHandle(id);
        _driver.BindVertexArray(handle);
        _boundVaoId = id;
    }

    public void BindVertexBuffer(VertexBufferId id)
    {
        if (_boundVertexBufferId == id) return;

        if (id == default)
        {
            _driver.BindVertexBuffer(default);

            _boundVertexBufferId = default;
            return;
        }

        var handle = _store.VboStore.GetHandle(id);
        _driver.BindVertexBuffer(handle);
        _boundVertexBufferId = id;
    }

    public void BindIndexBuffer(IndexBufferId id)
    {
        if (_boundIndexBufferId == id) return;

        if (id == default)
        {
            _driver.BindIndexBuffer(default);
            return;
        }

        var handle = _store.IboStore.GetHandle(id);
        _driver.BindIndexBuffer(handle);
        _boundIndexBufferId = id;
    }

    public void SetVertexAttribute(ReadOnlySpan<VertexAttributeDescriptor> attributes)
    {
        _boundVaoId.IsValidOrThrow();
        BindVertexBuffer(default);
         var vao =  _store.MeshStore.GetHandleAndMeta(_boundVaoId, out var meta);
        var meshLayout = _meshRegistry.GetInternal(_boundVaoId);
        var vboIds = meshLayout.VertexBufferIds;
        
        VertexBufferId prevVboId = default;
        for (int i = 0; i < attributes.Length; i++)
        {
            ref readonly var attrib = ref attributes[i];
            if (attrib.VboIndex > vboIds.Length)
                throw GraphicsException.InvalidState(
                    $"Attrib vbo index {attrib.VboIndex} is greater than vbo count {vboIds.Length}");

            var vboId = vboIds[attrib.VboIndex];
            if (prevVboId != vboId)
                BindVertexBuffer(vboId);

            _driver.SetVertexAttribute(vao, (uint)i, attrib);
            prevVboId = vboId;
        }
        var newMeta = MeshMeta.CreateCopy(in meta, (uint)attributes.Length, meta.DrawCount);
        _store.MeshStore.ReplaceMeta(_boundVaoId, in newMeta, out _);

    }

    public void SetVertexBuffer<T>(ReadOnlySpan<T> data, BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
    {
        _boundVertexBufferId.IsValidOrThrow();
        var vbo =  _store.VboStore.GetHandleAndMeta(_boundVertexBufferId, out var meta);
        
        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.ElementSize > 0)
            GraphicsException.ThrowInvalidBufferData<VertexBufferId>(_boundVertexBufferId.ToString(),
                "Buffer is static");

        var elementCount = data.Length;
        var elementSize = Unsafe.SizeOf<T>();
        nuint size = (nuint)(elementSize * elementCount);
        
        _driver.SetVertexBuffer(default, vbo, data, size, usage);

        var newMeta = new VertexBufferMeta(meta.Usage, meta.BindingIdx, (uint)elementCount, (uint)elementSize);
        _store.VboStore.ReplaceMeta(_boundVertexBufferId, in newMeta, out _);
    }

    public void SetIndexBuffer<T>(ReadOnlySpan<T> data, BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
    {
        _boundIndexBufferId.IsValidOrThrow();
        var vbo =  _store.IboStore.GetHandleAndMeta(_boundIndexBufferId, out var meta);
        
        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.ElementSize > 0)
            GraphicsException.ThrowInvalidBufferData<IndexBufferId>(_boundIndexBufferId.ToString(),
                "Buffer is static");

        var elementCount = data.Length;
        var elementSize = Unsafe.SizeOf<T>();
        nuint size = (nuint)(elementSize * elementCount);
        
        _driver.SetVertexBuffer(default, vbo, data, size, usage);

        var newMeta = new IndexBufferMeta(meta.Usage, (uint)elementCount, (uint)elementSize);
        _store.IboStore.ReplaceMeta(_boundIndexBufferId, in newMeta, out _);
    }

    public void UploadVertexBuffer<T>(VertexBufferId vb, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        var handle = _store.VboStore.GetHandle(vb);
        var byteOffset = (nuint)(offsetElements * Unsafe.SizeOf<T>());
        _driver.UploadVertexBuffer(handle, data, byteOffset);
    }

    public void UploadIndexBuffer<T>(IndexBufferId ib, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        var handle = _store.IboStore.GetHandle(ib);
        var byteOffset = (nuint)(offsetElements * Unsafe.SizeOf<T>());
        _driver.UploadIndexBuffer(handle, data, byteOffset);
    }

    
    public void SetUniformBufferSize(UniformGpuSlot slot, nuint capacityBytes)
    {
        var ubo = _shaderRegistry.GetUboId(slot);
        var handle = _store.UboStore.GetHandle(ubo);
        _driver.SetUniformBufferSize(slot, capacityBytes);
    }

    public void UploadUniformGpuData<T>(UniformGpuSlot slot, in T data, nuint offsetBytes = 0)
        where T : unmanaged, IUniformGpuData
    {
        var ubo = _shaderRegistry.GetUboId(slot);
        var handle = _store.UboStore.GetHandle(ubo);
        _driver.UploadUbo(handle, data, offsetBytes);
    }

    public void BindUniformBufferRange(UniformGpuSlot slot, nuint offset, nuint size)
    {
        var ubo = _shaderRegistry.GetUboId(slot);
        var handle = _store.UboStore.GetHandle(ubo);
        _driver.BindUniformBufferRange(handle, slot, offset, size);
    }
    
    public void DrawArrays(DrawPrimitive primitive, uint drawCount)
    {
        Debug.Assert(_boundVaoId.IsValid(), "No VAO is bound");
        Debug.Assert(drawCount != 0, "DrawArrays called with drawCount = 0");
        _driver.DrawArrays(primitive, drawCount);
        _drawTriangleCount += (int)drawCount;
        _drawCallCount++;
    }

    public void DrawElements(DrawPrimitive primitive, uint drawCount, DrawElementType elementType)
    {
        Debug.Assert(_boundVaoId.IsValid(), "No VAO is bound");
        Debug.Assert(drawCount != 0, "DrawElements called with drawCount = 0");
        _driver.DrawElements(primitive, drawCount, elementType);
        _drawTriangleCount += (int)drawCount;
        _drawCallCount++;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, int value) => _driver.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, uint value) => _driver.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, float value) => _driver.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector2 value) => _driver.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector3 value) => _driver.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(ShaderUniform uniform, Vector4 value) => _driver.SetUniform(_boundUniforms![uniform], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetUniform(ShaderUniform uniform, in Matrix4x4 value) =>
        _driver.SetUniform(_boundUniforms![uniform], in value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetUniform(ShaderUniform uniform, in Matrix3 value) =>
        _driver.SetUniform(_boundUniforms![uniform], in value);
    

}