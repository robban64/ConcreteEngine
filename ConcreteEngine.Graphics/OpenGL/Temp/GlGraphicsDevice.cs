#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlGraphicsDevice : IGraphicsDevice<GlGraphicsContext>
{
    #region Stores

    private const int StoreTier1 = 64;
    private const int StoreTier2 = 32;
    private const int StoreTier3 = 16;

    private readonly ResourceStore<TextureId, TextureMeta, GlTextureHandle> _textureStore = new(
        initialCapacity: StoreTier1, static i => new TextureId(i + 1));

    private readonly ResourceStore<ShaderId, ShaderMeta, GlShaderHandle> _shaderStore = new(
        initialCapacity: StoreTier2, static i => new ShaderId(i + 1));

    private readonly ResourceStore<MeshId, MeshMeta, GlMeshHandle> _meshStore = new(initialCapacity: StoreTier2,
        static i => new MeshId(i + 1));

    private readonly ResourceStore<VertexBufferId, VertexBufferMeta, GlVboHandle> _vboStore = new(
        initialCapacity: StoreTier2, static i => new VertexBufferId(i + 1));

    private readonly ResourceStore<IndexBufferId, IndexBufferMeta, GlIboHandle> _iboStore = new(
        initialCapacity: StoreTier2, static i => new IndexBufferId(i + 1));

    private readonly ResourceStore<FrameBufferId, FrameBufferMeta, GlFboHandle> _fboStore = new(
        initialCapacity: StoreTier3, static i => new FrameBufferId(i + 1));

    private readonly ResourceStore<RenderBufferId, RenderBufferMeta, GlRboHandle> _rboStore = new(
        initialCapacity: StoreTier3, static i => new RenderBufferId(i + 1));

    private readonly ResourceStore<UniformBufferId, UniformBufferMeta, GlUboHandle> _uboStore = new(
        initialCapacity: StoreTier3, static i => new UniformBufferId(i + 1));

    #endregion

    private readonly GL _gl;
    private readonly GlGraphicsContext _gfx;
    private readonly GlResourceFactory _resourceFactory;
    private readonly GlShaderFactory _shaderFactory;
    private readonly MeshFactory _meshFactory;

    private readonly ShaderRegistry _shaderRegistry;
    private readonly MeshRegistry _meshRegistry;

    private readonly ResourceDisposeQueue _disposeQueue;

    private readonly PrimitiveMeshes _primitives;

    private FrameInfo _prevFrameCtx;
    private FrameInfo _frameCtx;

    public GraphicsConfiguration Configuration { get; }

    public GL Gl => _gl;
    public GlGraphicsContext Gfx => _gfx;
    public GraphicsBackend BackendApi => GraphicsBackend.OpenGL;
    IGraphicsContext IGraphicsDevice.Gfx => Gfx;
    public IPrimitiveMeshes Primitives => _primitives;

    public IShaderRegistry ShaderRegistry => _shaderRegistry;
    public IMeshRegistry MeshRegistry => _meshRegistry;
    public IMeshFactory MeshFactory => _meshFactory;

    public GlGraphicsDevice(GL gl, in FrameInfo initialFrameCtx)
    {
        _gl = gl;
        _frameCtx = initialFrameCtx;
        _prevFrameCtx = initialFrameCtx;
        var capabilities = CreateDeviceCapabilities(gl);
        Configuration = new GraphicsConfiguration();
        UniformBufferUtils.Init(capabilities.UniformBufferOffsetAlignment);

        //_targetRegistry = new RenderTargetRegistry();
        _shaderRegistry = new ShaderRegistry(_uboStore);
        _meshRegistry = new MeshRegistry();
        _disposeQueue = new ResourceDisposeQueue();

        var contextBindingView = new GlResourceStoreView(textureStore: _textureStore, shaderStore: _shaderStore,
            meshStore: _meshStore, vboStore: _vboStore, iboStore: _iboStore, fboStore: _fboStore, rboStore: _rboStore,
            uboStore: _uboStore, _meshRegistry, _shaderRegistry);

        _gfx = new GlGraphicsContext(gl, capabilities, Configuration, contextBindingView, _shaderRegistry,
            in initialFrameCtx);

        _resourceFactory = new GlResourceFactory(_gfx, capabilities);
        _shaderFactory = new GlShaderFactory(_gfx, capabilities);
        _meshFactory = new MeshFactory(this, contextBindingView);

        _primitives = new PrimitiveMeshes();


        Console.WriteLine($"OpenGL version {capabilities.GlVersion} loaded.");
        Console.WriteLine("--Device Capability--");
        Console.WriteLine(capabilities.ToString());
    }

    public GpuResourceBuilder CreateBuilder() => new();
    public IGpuUploadSink CreateUploader() => new GpuUploadSink(this);

    public void BuildResources(GpuResourceBuilder builder)
    {
        _primitives.CreatePrimitives(this);
        builder.Apply(this);
    }


    public void StartFrame(in FrameInfo frameCtx)
    {
        _frameCtx = frameCtx;
        _gfx.BeginFrame(in frameCtx);
    }

    public void EndFrame(out GpuFrameStats result)
    {
        _disposeQueue.Drain(DisposeResource);

        _gfx.EndFrame(out result);

        // drain old resource
        _disposeQueue.Drain(DisposeResource);

        // After (_disposeQueue.Drain) so it get disposed next frame
        // TODO use a special tick or timer for disposing and recreating
        if (_frameCtx.ResizePending)
        {
            RecreateRenderTargets();
        }

        _prevFrameCtx = _frameCtx;
    }

    private void RecreateRenderTargets()
    {
        if (_prevFrameCtx.OutputSize == _frameCtx.OutputSize) return;

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        var (prev, curr) = (_prevFrameCtx.OutputSize, _frameCtx.OutputSize);
        Console.WriteLine($"New viewport {curr} - old viewport {prev}");
        Console.WriteLine($"Recreating {_fboStore.Count} FBO");

        for (int i = 0; i < _fboStore.Count; i++)
        {
            ReplaceFramebuffer(new FrameBufferId(i + 2));
        }
    }

    private void ReplaceFramebuffer(FrameBufferId fboId)
    {
        ref readonly var prevMeta = ref _fboStore.GetMeta(fboId);
        var outputSize = _frameCtx.OutputSize;
        var size = new Vector2D<int>((int)(outputSize.X * prevMeta.SizeRatio.X),
            (int)(outputSize.Y * prevMeta.SizeRatio.Y));

        Debug.Assert(size.X > 0);
        Debug.Assert(size.Y > 0);

        var (colTexId, rboTexId, rboDepthId) = (prevMeta.ColTexId, prevMeta.RboTexId, prevMeta.RboDepthId);

        TextureMeta colTexMeta = default;
        RenderBufferMeta rboTexMeta = default, rboDepthMeta = default;
        EnqueueRemoveResource(fboId, reserve: true);

        if (colTexId.Id > 0)
        {
            ref readonly var prevTexMeta = ref _textureStore.GetMeta(colTexId);
            colTexMeta = new TextureMeta(size.X, size.Y, prevTexMeta.Format);
        }

        if (rboTexId.Id > 0)
        {
            ref readonly var prevRboMeta = ref _rboStore.GetMeta(rboTexId);
            rboTexMeta = new RenderBufferMeta(prevRboMeta.Kind, size, prevRboMeta.Multisample);
        }

        if (rboDepthId.Id > 0)
        {
            ref readonly var prevRboMeta = ref _rboStore.GetMeta(rboDepthId);
            rboDepthMeta = new RenderBufferMeta(prevRboMeta.Kind, size, prevRboMeta.Multisample);
        }

        FrameBufferUtils.GetMetaResizeCopy(in prevMeta, outputSize, out var fboMeta);

        var desc = new FrameBufferDesc(fboMeta.SizeRatio, size, fboMeta.DepthStencilBuffer, fboMeta.TexturePreset,
            fboMeta.Msaa, fboMeta.Samples);

        var handle = _resourceFactory.CreateFrameBuffer(
            ReplaceTexture,
            ReplaceRbo,
            outputSize,
            in prevMeta,
            in desc,
            out var meta
        );

        _fboStore.Replace(fboId, in meta, handle, out _);

        return;

        TextureId ReplaceTexture(TextureId id, in TextureMeta newMeta, GlTextureHandle newHandle) =>
            _textureStore.Replace(id, in newMeta, in newHandle, out _);

        RenderBufferId ReplaceRbo(RenderBufferId id, in RenderBufferMeta newMeta, GlRboHandle newHandle) =>
            _rboStore.Replace(id, in newMeta, in newHandle, out _);
    }


    public FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta)
    {
        Debug.Assert(_frameCtx.OutputSize != Vector2D<int>.Zero);

        var handle = _resourceFactory.CreateFrameBuffer(
            AddStoreTex,
            AddStoreRbo,
            _frameCtx.OutputSize,
            default,
            in desc,
            out meta
        );

        return _fboStore.Add(in meta, in handle);

        TextureId AddStoreTex(TextureId _, in TextureMeta newMeta, GlTextureHandle newHandle) =>
            _textureStore.Add(in newMeta, in newHandle);

        RenderBufferId AddStoreRbo(RenderBufferId _, in RenderBufferMeta newMeta, GlRboHandle newHandle) =>
            _rboStore.Add(in newMeta, in newHandle);
    }

    public ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta)
    {
        var handle = _shaderFactory.CreateShader(vertexSource, fragmentSource,
            out var uniformTable, out meta);

        var shaderId = _shaderStore.Add(in meta, in handle);

        _shaderRegistry.Add(shaderId, uniformTable);
        return shaderId;
    }

    public TextureId CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta)
    {
        var handle = _resourceFactory.CreateTexture2D(data, in desc, out meta);
        return _textureStore.Add(in meta, handle);
    }

    public TextureId CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta)
    {
        var handle = _resourceFactory.CreateCubeMap(data, in desc, out meta);
        return _textureStore.Add(in meta, handle);
    }


    public MeshId CreateMesh(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement)
    {
        var handle = _resourceFactory.CreateVao();
        var meta = new MeshMeta(primitive, drawKind, drawElement, 0, 0);
        var meshId = _meshStore.Add(in meta, handle);
        _meshRegistry.RegisterEmptyMesh(meshId);
        return meshId;
    }

    public VertexBufferId CreateVertexBuffer(BufferUsage usage, uint elementSize, uint bindingIndex = 0)
    {
        var handle = _resourceFactory.CreateVertexBuffer();
        var vboId = _vboStore.Add(new VertexBufferMeta(usage, bindingIndex, 0, elementSize), handle);
        return vboId;
    }

    public IndexBufferId CreateIndexBuffer(BufferUsage usage, uint elementSize)
    {
        var handle = _resourceFactory.CreateIndexBuffer();
        return _iboStore.Add(new IndexBufferMeta(usage, 0, elementSize), handle);
    }

    public UniformBufferId CreateUniformBuffer<T>(UniformGpuSlot slot, UboDefaultCapacity defaultCapacity,
        out UniformBufferMeta meta) where T : unmanaged, IUniformGpuData
    {
        var result = _shaderFactory.CreateUniformBuffer<T>(slot, defaultCapacity, out meta);
        var uboId = _uboStore.Add(in meta, result);
        _shaderRegistry.AddUboToSlot(meta.Slot, uboId);
        return uboId;
    }


    public void EnqueueRemoveResource<TId>(TId id, bool reserve = false) where TId : unmanaged, IResourceId
    {
        Console.WriteLine($"Enqueue removal of {typeof(TId).Name}");
        switch (id)
        {
            case TextureId textureId:
                var handleTex = _textureStore.GetHandle(textureId).Handle;
                _disposeQueue.Enqueue(ResourceKind.Texture, handleTex);
                if (!reserve) _textureStore.Remove(textureId, out _);
                break;
            case ShaderId shaderId:
                var handleShader = _shaderStore.GetHandle(shaderId).Handle;
                _disposeQueue.Enqueue(ResourceKind.Shader, handleShader);
                if (!reserve) _shaderStore.Remove(shaderId, out _);

                break;
            case MeshId meshId:
                ref readonly var mesh = ref _meshStore.GetMeta(meshId);
                var handleMesh = _meshStore.GetHandle(meshId).Handle;
                
                var meshLayout = _meshRegistry.GetInternal(meshId);
                if (meshLayout.IndexBufferId.IsValid())
                    EnqueueRemoveResource(meshLayout.IndexBufferId, reserve);
                
                var vboIds = meshLayout.GetVertexBufferIds();
                foreach (var vboId in vboIds)
                    EnqueueRemoveResource(vboId, reserve);
                
                _disposeQueue.Enqueue(ResourceKind.Mesh, handleMesh);
                if (!reserve) _meshStore.Remove(meshId, out _);
                break;
            case VertexBufferId vboId:
                var handleVbo = _vboStore.GetHandle(vboId).Handle;
                _disposeQueue.Enqueue(ResourceKind.VertexBuffer, handleVbo);
                if (!reserve) _vboStore.Remove(vboId, out _);
                break;
            case IndexBufferId iboId:
                var handleIbo = _iboStore.GetHandle(iboId).Handle;
                _disposeQueue.Enqueue(ResourceKind.IndexBuffer, handleIbo);
                if (!reserve) _iboStore.Remove(iboId, out _);
                break;
            case FrameBufferId fboId:
                ref readonly var fbo = ref _fboStore.GetMeta(fboId);
                if (fbo.RboDepthId.Id > 0) EnqueueRemoveResource(fbo.RboDepthId, reserve);
                if (fbo.RboTexId.Id > 0) EnqueueRemoveResource(fbo.RboTexId, reserve);
                if (fbo.ColTexId.Id > 0) EnqueueRemoveResource(fbo.ColTexId, reserve);

                var fboHandle = _fboStore.GetHandle(fboId).Handle;
                _disposeQueue.Enqueue(ResourceKind.FrameBuffer, fboHandle);
                if (!reserve) _fboStore.Remove(fboId, out _);
                break;
            case RenderBufferId rboId:
                var rboHandle = _rboStore.GetHandle(rboId).Handle;
                _disposeQueue.Enqueue(ResourceKind.RenderBuffer, rboHandle);
                if (!reserve) _rboStore.Remove(rboId, out _);
                break;
            default:
                throw new GraphicsException($"Unknown resource type {typeof(TId).Name}");
        }
    }

    private void DisposeResource(ResourceKind kind, uint handle)
    {
        switch (kind)
        {
            case ResourceKind.Texture: DisposeTexture(handle); break;
            case ResourceKind.Shader: DisposeShader(handle); break;
            case ResourceKind.Mesh: DisposeVao(handle); break;
            case ResourceKind.VertexBuffer: DisposeVbo(handle); break;
            case ResourceKind.IndexBuffer: DisposeIbo(handle); break;
            case ResourceKind.FrameBuffer: DisposeFbo(handle); break;
            case ResourceKind.RenderBuffer: DisposeRbo(handle); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private void DisposeTexture(uint handle) => _gl.DeleteTexture(handle);
    private void DisposeShader(uint handle) => _gl.DeleteTexture(handle);
    private void DisposeVao(uint handle) => _gl.DeleteVertexArray(handle);
    private void DisposeVbo(uint handle) => _gl.DeleteBuffer(handle);
    private void DisposeIbo(uint handle) => _gl.DeleteBuffer(handle);
    private void DisposeFbo(uint handle) => _gl.DeleteFramebuffer(handle);
    private void DisposeRbo(uint handle) => _gl.DeleteRenderbuffer(handle);

    private static DeviceCapabilities CreateDeviceCapabilities(GL gl)
    {
        return new DeviceCapabilities
        {
            GlVersion = new OpenGlVersion(gl.GetInteger(GetPName.MajorVersion), gl.GetInteger(GetPName.MinorVersion)),
            MaxTextureImageUnits = gl.GetInteger(GLEnum.MaxCombinedTextureImageUnits),
            MaxVertexAttribBindings = gl.GetInteger((GLEnum)0x82DA), // GL_MAX_VERTEX_ATTRIB_BINDINGS
            MaxTextureSize = gl.GetInteger(GLEnum.MaxTextureSize),
            MaxArrayTextureLayers = gl.GetInteger(GLEnum.MaxArrayTextureLayers),
            MaxFramebufferWidth = gl.GetInteger((GLEnum)0x9315), // GL_MAX_FRAMEBUFFER_WIDTH
            MaxFramebufferHeight = gl.GetInteger((GLEnum)0x9316), // GL_MAX_FRAMEBUFFER_HEIGHT
            MaxSamples = gl.GetInteger(GLEnum.MaxSamples),
            MaxColorAttachments = gl.GetInteger(GLEnum.MaxColorAttachments),
            MaxAnisotropy = gl.GetFloat(GLEnum.MaxTextureMaxAnisotropy),
            MaxUniformBlockSize = gl.GetInteger(GLEnum.MaxUniformBlockSize),
            UniformBufferOffsetAlignment = gl.GetInteger(GetPName.UniformBufferOffsetAlignment),
        };
    }

    public void Dispose()
    {
        /*
        Console.WriteLine(
            $"{nameof(GlGraphicsDevice)} Disposing {nameof(GlGraphicsDevice)} with {_store.Count} resources");

        int counter = 0;
        for (ushort i = 1; i < _store.Count; i++)
        {
            var resource = _store.Get(i);
            if (resource == null || resource.IsDisposed) continue;
            FreeResource(resource);
            counter++;
        }

        Console.WriteLine($"{nameof(GlGraphicsDevice)} Disposing finished");
        Console.WriteLine($"Total of {counter} resources directly");

        _gl.Dispose();
        */
    }
}