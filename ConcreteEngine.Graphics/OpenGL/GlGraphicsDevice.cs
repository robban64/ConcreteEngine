#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
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
        initialCapacity: StoreTier1,
        i => new TextureId(i + 1)
    );

    private readonly ResourceStore<ShaderId, ShaderMeta, GlShaderHandle> _shaderStore = new(
        initialCapacity: StoreTier2,
        i => new ShaderId(i + 1)
    );

    private readonly ResourceStore<MeshId, MeshMeta, GlMeshHandle> _meshStore = new(
        initialCapacity: StoreTier2,
        i => new MeshId(i + 1)
    );

    private readonly ResourceStore<VertexBufferId, VertexBufferMeta, GlVertexBufferHandle> _vboStore = new(
        initialCapacity: StoreTier2,
        i => new VertexBufferId(i + 1)
    );

    private readonly ResourceStore<IndexBufferId, IndexBufferMeta, GlIndexBufferHandle> _iboStore = new(
        initialCapacity: StoreTier2,
        i => new IndexBufferId(i + 1)
    );

    private readonly ResourceStore<FrameBufferId, FrameBufferMeta, GlFrameBufferHandle> _fboStore = new(
        initialCapacity: StoreTier3,
        i => new FrameBufferId(i + 1)
    );

    private readonly ResourceStore<RenderBufferId, RenderBufferMeta, GlRenderBufferHandle> _rboStore = new(
        initialCapacity: StoreTier3,
        i => new RenderBufferId(i + 1)
    );

    #endregion


    private readonly GL _gl;
    private readonly GlGraphicsContext _gfx;
    private readonly GlResourceFactory _resourceFactory;
    private readonly UniformRegistry _uniformRegistry;
    private readonly ResourceDisposeQueue _disposeQueue;

    private readonly MeshId _quadMesh;

    private Vector2D<int> _previousViewportSize;
    private Vector2D<int> _viewportSize;

    public GraphicsConfiguration Configuration { get; }

    public GL Gl => _gl;
    public GlGraphicsContext Gfx => _gfx;
    public GraphicsBackend BackendApi => GraphicsBackend.OpenGL;
    public MeshId QuadMeshId => _quadMesh;
    IGraphicsContext IGraphicsDevice.Gfx => Gfx;

    public GlGraphicsDevice(GL gl, in GraphicsFrameContext initialFrameCtx)
    {
        _gl = gl;
        _viewportSize = initialFrameCtx.ViewportSize;
        _previousViewportSize = initialFrameCtx.ViewportSize;
        Configuration = new GraphicsConfiguration(CreateDeviceCapabilities(gl));

        //_targetRegistry = new RenderTargetRegistry();
        _uniformRegistry = new UniformRegistry();
        _disposeQueue = new ResourceDisposeQueue();

        var contextBindingView = new GlContextBindingView(
            textureStore: _textureStore,
            shaderStore: _shaderStore,
            meshStore: _meshStore,
            vboStore: _vboStore,
            iboStore: _iboStore,
            fboStore: _fboStore,
            rboStore: _rboStore
        );
        _gfx = new GlGraphicsContext(gl, Configuration, contextBindingView, _uniformRegistry, in initialFrameCtx);

        _resourceFactory = new GlResourceFactory(_gfx);

        Console.WriteLine($"OpenGL version {Configuration.Capabilities.GlVersion} loaded.");
        Console.WriteLine("--Device Capability--");
        Console.WriteLine(Configuration.Capabilities.ToString());

        _quadMesh = CreateMesh(new MeshDescriptor<Vertex2D, uint>
        {
            VertexBuffer = new MeshDataBufferDescriptor<Vertex2D>(BufferUsage.StaticDraw, Quad.Vertices),
            IndexBuffer = null,
            VertexPointers =
            [
                VertexAttributeDescriptor.Make<Vertex2D>(nameof(Vertex2D.Position)),
                VertexAttributeDescriptor.Make<Vertex2D>(nameof(Vertex2D.Texture))
            ],
            Primitive = DrawPrimitive.TriangleStrip
        }, out _);
    }

    public void StartFrame(in GraphicsFrameContext frameCtx)
    {
        _viewportSize = frameCtx.ViewportSize;
        _gfx.BeginFrame(in frameCtx);
    }

    public void EndFrame(out GraphicsFrameResult result)
    {
        _gfx.EndFrame(out result);

        // drain old resource
        _disposeQueue.Drain();

        // After (_disposeQueue.Drain) so it get disposed next frame
        // TODO use a special tick or timer for disposing and recreating
        RecreateRenderTargetsIfNeeded();
    }

    public UniformTable GetShaderUniforms(ShaderId shaderId)
    {
        return _uniformRegistry.Get(shaderId);
    }

    private void RecreateRenderTargetsIfNeeded()
    {
        if (_viewportSize == _previousViewportSize) return;
        _previousViewportSize = _viewportSize;

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        Console.WriteLine($"New viewport {_viewportSize} - old viewport {_previousViewportSize}");
        Console.WriteLine($"Recreating {_fboStore.Count} FBO");

        for (int i = 0; i < _fboStore.Count; i++)
        {
            ReplaceFramebuffer(new FrameBufferId(i + 2));
        }
    }

    private FrameBufferMeta ReplaceFramebuffer(FrameBufferId fboId)
    {
        ref readonly var prevMeta = ref _fboStore.GetMeta(fboId);
        var size = new Vector2D<int>((int)(_viewportSize.X * prevMeta.SizeRatio.X),
            (int)(_viewportSize.Y * prevMeta.SizeRatio.Y));

        var (colTexId, rboTexId, rboDepthId) = (prevMeta.ColTexId, prevMeta.RboTexId, prevMeta.RboDepthId);

        TextureMeta colTexMeta = default;
        RenderBufferMeta rboTexMeta = default, rboDepthMeta = default;
        EnqueueRemoveResource(fboId, reserve: true);

        if (colTexId.Id > 0)
        {
            ref readonly var prevTexMeta = ref _textureStore.GetMeta(colTexId);
            colTexMeta = new TextureMeta(size, prevTexMeta.Format);
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

        FrameBufferMeta.GetResizeCopy(in prevMeta, _viewportSize, out var fboMeta);
        var desc = new FrameBufferDesc(prevMeta.SizeRatio, size, prevMeta.DepthStencilBuffer, prevMeta.TexturePreset,
            prevMeta.Msaa, prevMeta.Samples);

        var handle = _resourceFactory.CreateFrameBuffer(
            (handle, m) => _textureStore.Replace(colTexId, in colTexMeta, handle, out _),
            (handle, m) => _rboStore.Replace(rboTexId, in rboTexMeta, handle, out _),
            (handle, m) => _rboStore.Replace(rboDepthId, in rboDepthMeta, handle, out _),
            _viewportSize,
            in desc,
            out var meta
        );

        _fboStore.Replace(fboId, in meta, handle, out _);
        return meta;
    }

    public FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta)
    {
        var handle = _resourceFactory.CreateFrameBuffer(
            (handle, m) => _textureStore.Add(in m, in handle),
            (handle, m) => _rboStore.Add(in m, in handle),
            (handle, m) => _rboStore.Add(in m, in handle),
            _viewportSize,
            in desc,
            out meta
        );

        return _fboStore.Add(in meta, in handle);
    }

    public ShaderId CreateShader(string vertexSource, string fragmentSource, string[] samplers)
    {
        var handle = _resourceFactory.CreateShader(vertexSource, fragmentSource, samplers,
            out var uniformTable, out var meta);


        var shaderId = _shaderStore.Add(in meta, in handle);

        _uniformRegistry.Add(shaderId, uniformTable);
        return shaderId;
    }

    public TextureId CreateTexture2D(in TextureDesc textureDesc)
    {
        var handle = _resourceFactory.CreateTexture2D(in textureDesc, out var meta);
        return _textureStore.Add(in meta, handle);
    }

    public MeshId CreateMesh<TVertex, TIndex>(MeshDescriptor<TVertex, TIndex> descriptor, out MeshMeta meta)
        where TVertex : unmanaged where TIndex : unmanaged
    {
        var handle = _resourceFactory.CreateMesh(
            (handle, m) => _vboStore.Add(in m, in handle),
            (handle, m) => _iboStore.Add(in m, in handle),
            descriptor, out meta);

        return _meshStore.Add(in meta, handle);
    }

    public VertexBufferId CreateVertexBuffer(BufferUsage usage)
    {
        var handle = _resourceFactory.CreateVertexBuffer();
        return _vboStore.Add(new VertexBufferMeta(usage, 0, 0), handle);
    }

    public IndexBufferId CreateIndexBuffer(BufferUsage usage, IboElementType elementType)
    {
        var handle = _resourceFactory.CreateIndexBuffer();
        return _iboStore.Add(new IndexBufferMeta(usage, 0, 0), handle);
    }


    public void EnqueueRemoveResource<TId>(TId id, bool reserve = false) where TId : struct
    {
        Console.WriteLine($"Enqueue removal of {typeof(TId).Name}");
        switch (id)
        {
            case TextureId textureId:
                var handleTex = _textureStore.GetHandle(textureId).Handle;
                _disposeQueue.Enqueue(ResourceKind.Texture, () => FreeTexture(handleTex));
                if (!reserve) _textureStore.Remove(textureId, out _);
                break;
            case ShaderId shaderId:
                var handleShader = _shaderStore.GetHandle(shaderId).Handle;
                _disposeQueue.Enqueue(ResourceKind.Shader, () => FreeShader(handleShader));
                if (!reserve) _shaderStore.Remove(shaderId, out _);

                break;
            case MeshId meshId:
                ref readonly var mesh = ref _meshStore.GetMeta(meshId);
                var handleMesh = _meshStore.GetHandle(meshId).Handle;
                if (mesh.VertexBufferId.Id > 0) EnqueueRemoveResource(mesh.VertexBufferId, reserve);
                if (mesh.IndexBufferId.Id > 0) EnqueueRemoveResource(mesh.IndexBufferId, reserve);
                _disposeQueue.Enqueue(ResourceKind.Mesh, () => FreeMesh(handleMesh));
                if (!reserve) _meshStore.Remove(meshId, out _);
                break;
            case VertexBufferId vboId:
                var handleVbo = _vboStore.GetHandle(vboId).Handle;
                _disposeQueue.Enqueue(ResourceKind.VertexBuffer, () => FreeVbo(handleVbo));
                if (!reserve) _vboStore.Remove(vboId, out _);
                break;
            case IndexBufferId iboId:
                var handleIbo = _iboStore.GetHandle(iboId).Handle;
                _disposeQueue.Enqueue(ResourceKind.IndexBuffer, () => FreeIbo(handleIbo));
                if (!reserve) _iboStore.Remove(iboId, out _);
                break;
            case FrameBufferId fboId:
                ref readonly var fbo = ref _fboStore.GetMeta(fboId);
                if (fbo.RboDepthId.Id > 0) EnqueueRemoveResource(fbo.RboDepthId, reserve);
                if (fbo.RboTexId.Id > 0) EnqueueRemoveResource(fbo.RboTexId, reserve);
                if (fbo.ColTexId.Id > 0) EnqueueRemoveResource(fbo.ColTexId, reserve);
                var fboHandle = _fboStore.GetHandle(fboId).Handle;
                _disposeQueue.Enqueue(ResourceKind.FrameBuffer, () => FreeFbo(fboHandle));
                if (!reserve) _fboStore.Remove(fboId, out _);
                break;
            case RenderBufferId rboId:
                var rboHandle = _rboStore.GetHandle(rboId).Handle;
                _disposeQueue.Enqueue(ResourceKind.RenderBuffer, () => FreeRbo(rboHandle));
                if (!reserve) _rboStore.Remove(rboId, out _);
                break;
            default:
                throw new GraphicsException($"Unknown resource type {typeof(TId).Name}");
        }
    }


    private void FreeTexture(uint handle) => _gl.DeleteTexture(handle);
    private void FreeShader(uint handle) => _gl.DeleteTexture(handle);
    private void FreeMesh(uint handle) => _gl.DeleteVertexArray(handle);
    private void FreeVbo(uint handle) => _gl.DeleteBuffer(handle);
    private void FreeIbo(uint handle) => _gl.DeleteBuffer(handle);
    private void FreeFbo(uint handle) => _gl.DeleteFramebuffer(handle);
    private void FreeRbo(uint handle) => _gl.DeleteRenderbuffer(handle);

/*
    private void FreeTexture(TextureId id)
    {
        var handle = _textureStore.GetHandle(id);
        _gl.DeleteTexture(handle.Handle);
    }

    private void FreeShader(ShaderId id)
    {
        var handle = _shaderStore.GetHandle(id);
        _gl.DeleteTexture(handle.Handle);
    }

    private void FreeMesh(MeshId id)
    {
        var handle = _meshStore.GetHandle(id);
        _gl.DeleteVertexArray(handle.Handle);
    }

    private void FreeVbo(VertexBufferId id)
    {
        var handle = _vboStore.GetHandle(id);
        _gl.DeleteBuffer(handle.Handle);
    }

    private void FreeIbo(IndexBufferId id)
    {
        var handle = _iboStore.GetHandle(id);
        _gl.DeleteBuffer(handle.Handle);
    }

    private void FreeFbo(FrameBufferId id)
    {
        var handle = _fboStore.GetHandle(id);
        _gl.DeleteFramebuffer(handle.Handle);
    }

    private void FreeRbo(RenderBufferId id)
    {
        var handle = _rboStore.GetHandle(id);
        _gl.DeleteRenderbuffer(handle.Handle);
    }
*/
    private static DeviceCapabilities CreateDeviceCapabilities(GL gl)
    {
        return new DeviceCapabilities
        {
            GlVersion = new OpenGlVersion(gl.GetInteger(GetPName.MajorVersion), gl.GetInteger(GetPName.MinorVersion)),
            MaxTextureImageUnits = gl.GetInteger(GLEnum.MaxCombinedTextureImageUnits),
            MaxVertexAttribBindings = gl.GetInteger((GLEnum)0x82DA), // GL_MAX_VERTEX_ATTRIB_BINDINGS
            MaxTextureSize = gl.GetInteger(GLEnum.MaxTextureSize),
            MaxArrayTextureLayers = gl.GetInteger(GLEnum.MaxArrayTextureLayers),
            MaxUniformBlockSize = gl.GetInteger(GLEnum.MaxUniformBlockSize),
            MaxFramebufferWidth = gl.GetInteger((GLEnum)0x9315), // GL_MAX_FRAMEBUFFER_WIDTH
            MaxFramebufferHeight = gl.GetInteger((GLEnum)0x9316), // GL_MAX_FRAMEBUFFER_HEIGHT
            MaxSamples = gl.GetInteger(GLEnum.MaxSamples),
            MaxColorAttachments = gl.GetInteger(GLEnum.MaxColorAttachments)
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