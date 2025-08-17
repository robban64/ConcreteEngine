using System.Drawing;
using System.Numerics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlGraphicsDevice : IGraphicsDevice<GlGraphicsContext>
{
    private readonly GL _gl;
    private readonly GlGraphicsContext _gfx;
    private readonly GlResourceFactory _resourceFactory;

    private readonly GraphicsResourceStore _store;

    //private readonly RenderTargetRegistry _targetRegistry;
    private readonly UniformRegistry _uniformRegistry;
    private readonly ResourceDisposeQueue _disposeQueue;

    private readonly ushort _quadMesh;


    private Vector2D<int> _previousViewportSize;
    private Vector2D<int> _viewportSize;


    public GraphicsConfiguration Configuration { get; }

    public GL Gl => _gl;
    public GlGraphicsContext Gfx => _gfx;
    public GraphicsBackend BackendApi => GraphicsBackend.OpenGL;
    public ushort QuadMeshId => _quadMesh;
    IGraphicsContext IGraphicsDevice.Gfx => Gfx;

    public GlGraphicsDevice(GL gl, in GraphicsFrameContext initialFrameCtx)
    {
        _gl = gl;
        _viewportSize = initialFrameCtx.ViewportSize;
        _previousViewportSize = initialFrameCtx.ViewportSize;
        Configuration = new GraphicsConfiguration(CreateDeviceCapabilities(gl));

        _store = new GraphicsResourceStore();
        //_targetRegistry = new RenderTargetRegistry();
        _uniformRegistry = new UniformRegistry();
        _disposeQueue = new ResourceDisposeQueue(FreeResource);

        _gfx = new GlGraphicsContext(gl, Configuration, _store, _uniformRegistry, in initialFrameCtx);

        _resourceFactory = new GlResourceFactory(_gfx);

        Console.WriteLine($"OpenGL version {Configuration.Capabilities.GlVersion} loaded.");
        Console.WriteLine("--Device Capability--");
        Console.WriteLine(Configuration.Capabilities.ToString());

        var quadMeshResult = CreateMesh(new MeshDescriptor<Vertex2D, uint>
        {
            VertexBuffer = new MeshDataBufferDescriptor<Vertex2D>(BufferUsage.StaticDraw, Quad.Vertices),
            IndexBuffer = null,
            VertexPointers =
            [
                VertexAttributeDescriptor.Make<Vertex2D>("aPos", nameof(Vertex2D.Position)),
                VertexAttributeDescriptor.Make<Vertex2D>("aTex", nameof(Vertex2D.Texture))
            ],
            Primitive = PrimitiveType.TriangleStrip
        });

        _quadMesh = quadMeshResult.MeshId;
    }

    public void StartFrame(in GraphicsFrameContext frameCtx)
    {
        _viewportSize = frameCtx.ViewportSize;
        _gfx.BeginFrame(in frameCtx);
    }

    public void EndFrame()
    {
        _gfx.EndFrame();

        // drain old resource
        _disposeQueue.Drain();

        // After (_disposeQueue.Drain) so it get disposed next frame
        // TODO use a special tick or timer for disposing and recreating
        RecreateRenderTargetsIfNeeded();
    }
/*
    private void RecreateRenderTargetsIfNeeded()
    {
        if (_viewportSize == _previousViewportSize) return;
        _previousViewportSize = _viewportSize;

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        Console.WriteLine($"New viewport {_viewportSize} - old viewport {_previousViewportSize}");
        Console.WriteLine($"Recreating {_targetRegistry.Count} FBO");

        for (ushort i = 1; i < _targetRegistry.Count; i++)
        {
            var key = new RenderTargetKey(i);
            _targetRegistry.Get(key, out _); // validate
            ReplaceRenderTarget(key);
        }
    }

    public ushort GetRenderTargetTexture(ushort fboId)
    {
        return _store.Get<GlFramebuffer>(fboId).ColorTextureId;
    }

    public RenderTargetHandlerResult GetRenderTarget(RenderTargetKey key)
    {
        if (key.Key >= GraphicsConsts.MaxFboCount - 1)
            GraphicsException.ThrowCapabilityExceeded<GlGraphicsContext>(nameof(_targetRegistry),
                key.Key, GraphicsConsts.MaxFboCount);

        _targetRegistry.Get(key, out var target);
        if (target.Generation == 0)
            GraphicsException.ThrowResourceNotFound(key.Key);

        return new RenderTargetHandlerResult(target.FboId, target.ColTexId);
    }

    public RenderTargetKey CreateRenderTarget(RenderTargetId target, Vector2 sizeRatio)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeRatio.X, nameof(sizeRatio.X));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeRatio.Y, nameof(sizeRatio.Y));

        var view = _gfx.ViewportSize;
        var size = new Vector2D<int>((int)(view.X * sizeRatio.X), (int)(view.Y * sizeRatio.Y));

        var result = _resourceFactory.CreateFrameBuffer(size);

        var colTex = new GlTexture2D(result.Texture, size.X, size.Y, EnginePixelFormat.Rgba);
        var colTexId = _store.AddResource(colTex);

        var newFbo = new GlFramebuffer(result.Fbo, result.Renderbuffer, colTexId, size);
        var fboId = _store.AddResource(newFbo);

        var newTarget = new RenderTargetData(fboId, colTexId, target, 1, sizeRatio);
        var key = _targetRegistry.Add(in newTarget);
        return key;
    }

    public RenderTargetKey ReplaceRenderTarget(RenderTargetKey key)
    {
        _targetRegistry.Get(key, out var target);
        var (view, ratio) = (_viewportSize, target.SizeRatio);
        var size = new Vector2D<int>((int)(view.X * ratio.X), (int)(view.Y * ratio.Y));

        var createRes = _resourceFactory.CreateFrameBuffer(size);
        var newFbo = new GlFramebuffer(createRes.Fbo, createRes.Renderbuffer, target.ColTexId, size);
        var newTex = new GlTexture2D(createRes.Texture, size.X, size.Y, EnginePixelFormat.Rgba);

        var gen = (ushort)(target.Generation + 1);
        var updateTarget = target with { Generation = gen };

        _targetRegistry.Replace(key, in updateTarget, out _);

        _store.ReplaceResource<GlTexture2D>(target.ColTexId, newTex, out var prevTex);
        _store.ReplaceResource<GlFramebuffer>(target.FboId, newFbo, out var prevFbo);

        _disposeQueue.Enqueue(prevTex, target.ColTexId);
        _disposeQueue.Enqueue(prevFbo, target.FboId);

        return key;
    }
    */

    private void RecreateRenderTargetsIfNeeded()
    {
        if (_viewportSize == _previousViewportSize) return;
        _previousViewportSize = _viewportSize;

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        Console.WriteLine($"New viewport {_viewportSize} - old viewport {_previousViewportSize}");
        int counter = 0;
        for (ushort i = 1; i < _store.Count; i++)
        {
            if (!_store.TryGet<GlFramebuffer>(i, out var fbo)) continue;
            ReplaceFramebuffer(i);
            counter++;
        }

        Console.WriteLine($"Recreating {counter} FBO");
    }

    private (ushort, ushort) ReplaceFramebuffer(ushort fboId)
    {
        var prevFbo = _store.Get<GlFramebuffer>(fboId);
        var view = _viewportSize;

        var res = _resourceFactory.CreateFrameBuffer(view, prevFbo.Descriptor);
        var newFbo = new GlFramebuffer(res.Fbo, prevFbo.ColTextureId, res.Rbo, res.RboTexture, res.Size,
            prevFbo.SizeRatio, prevFbo.Descriptor);
        var newTex = new GlTexture2D(res.Texture, res.Size.X, res.Size.Y, EnginePixelFormat.Rgba);


        _store.ReplaceResource<GlTexture2D>(prevFbo.ColTextureId, newTex, out var prevTex);
        _store.ReplaceResource<GlFramebuffer>(fboId, newFbo, out var prevFbo2);

        _disposeQueue.Enqueue(prevTex, prevFbo.ColTextureId);
        _disposeQueue.Enqueue(prevFbo, fboId);


        return (fboId, newFbo.ColTextureId);
    }

    public (ushort, ushort) CreateFramebuffer(in FramebufferDescriptor desc)
    {
        var view = _viewportSize;
        var res = _resourceFactory.CreateFrameBuffer(_viewportSize, in desc);

        
        var newTex = new GlTexture2D(res.Texture, res.Size.X, res.Size.Y, EnginePixelFormat.Rgba);
        var texId = _store.AddResource(newTex);

        var newFbo = new GlFramebuffer(res.Fbo, texId, res.Rbo, res.RboTexture, res.Size, desc.SizeRatio, in desc);

        var fboId = _store.AddResource(newFbo);

        return (fboId, texId);
    }

    public ushort CreateShader(string vertexSource, string fragmentSource, string[] samplers)
    {
        var (resource, handle, uniformTable) = _resourceFactory.CreateShader(vertexSource, fragmentSource, samplers);
        _gl.UseProgram(handle);
        for (int i = 0; i < samplers.Length; i++)
            _gl.Uniform1(uniformTable.GetUniformLocation(samplers[i]), i);
        _gl.UseProgram(0);

        var resourceId = _store.AddResource(resource);

        _uniformRegistry.Add(resourceId, uniformTable);
        return resourceId;
    }

    public ushort CreateTexture2D(in TextureDescriptor textureDescriptor)
    {
        var resource = _resourceFactory.CreateTexture2D(in textureDescriptor);
        return _store.AddResource(resource);
    }

    public ushort CreateVertexBuffer(BufferUsage bufferUsage) => CreateBuffer(BufferTarget.VertexBuffer, bufferUsage);
    public ushort CreateIndexBuffer(BufferUsage bufferUsage) => CreateBuffer(BufferTarget.IndexBuffer, bufferUsage);

    public ushort CreateBuffer(BufferTarget target, BufferUsage usage)
    {
        var resource = _resourceFactory.CreateBuffer(target, usage);
        return _store.AddResource(resource);
    }


    public CreateMeshResult CreateMesh<TVertex, TIndex>(MeshDescriptor<TVertex, TIndex> meshData)
        where TVertex : unmanaged where TIndex : unmanaged
    {
        var resource = _resourceFactory.CreateMesh(this, meshData);
        var meshId = _store.AddResource(resource);
        return new CreateMeshResult(meshId, resource.VertexBufferId, resource.IndexBufferId, resource.DrawCount);
    }

    public void RemoveResource(ushort resourceId)
    {
        var resource = _store.Get(resourceId);

        _store.EnqueueRemoveResource(resourceId);

        if (resource is GlMesh mesh)
        {
            RemoveResource(mesh.VertexBufferId);
            if (mesh.IndexBufferId > 0)
                RemoveResource(mesh.IndexBufferId);
        }
        else if (resource is GlShader shader)
            _uniformRegistry.Remove(resourceId);

        //else if (resource is GlFramebuffer framebuffer)
            //RemoveResource(framebuffer.ColTextureHandle);
    }

    private void FreeResource(IGraphicsResource resource)
    {
        if (resource.IsDisposed) return;
        resource.IsDisposed = true;
        //Console.WriteLine($"Disposing {resource.GetType().Name}");
        switch (resource)
        {
            case GlShader shader:
                _gl.DeleteShader(shader.Handle);
                break;
            case GlTexture2D texture:
                _gl.DeleteTexture(texture.Handle);
                break;
            case GlBuffer buffer:
                _gl.DeleteBuffer(buffer.Handle);
                break;
            case GlRenderTarget renderTarget:
                _gl.DeleteFramebuffer(renderTarget.Handle);
                break;
            case GlMesh mesh:
                _gl.DeleteVertexArray(mesh.Handle);
                break;
            case GlFramebuffer fbo:
                if (fbo.RboHandle > 0) _gl.DeleteRenderbuffer(fbo.RboHandle);
                if (fbo.RboTextureHandle > 0) _gl.DeleteRenderbuffer(fbo.RboTextureHandle);
                if (fbo.ColTextureId > 0) _gl.DeleteTexture(fbo.ColTextureId);
                _gl.DeleteFramebuffer(fbo.Handle);
                break;
        }
    }

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