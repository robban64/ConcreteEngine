using System.Drawing;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlGraphicsDevice : IGraphicsDevice<GlGraphicsContext>
{
    private readonly GL _gl;
    private readonly GlResourceFactory _resourceFactory;
    private readonly GraphicsResourceStore _store;
    private readonly ushort _quadMesh;

    public GlGraphicsContext Ctx { get; }
    public GraphicsConfiguration Configuration { get; }

    public GL Gl => _gl;
    public GraphicsBackend BackendApi => GraphicsBackend.OpenGL;
    IGraphicsContext IGraphicsDevice.Ctx => Ctx;

    public GlGraphicsDevice(GL gl, in GraphicsFrameContext initialFrameCtx)
    {
        _gl = gl;
        Configuration = new GraphicsConfiguration(CreateDeviceCapabilities(gl));
        _store = new GraphicsResourceStore(FreeResource);
        Ctx = new GlGraphicsContext(gl, Configuration, _store, in initialFrameCtx);
        
        Console.WriteLine($"OpenGL version {Configuration.Capabilities.GlVersion} loaded.");
        Console.WriteLine("--Device Capability--");
        Console.WriteLine(Configuration.Capabilities.ToString());

        _resourceFactory = new GlResourceFactory(Ctx);
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

        Ctx.QuadMeshId = quadMeshResult.MeshId;
    }


    public void CleanupAfterRender()
    {
        _store.FlushRemoveQueue();
    }

    public ushort CreateShader(string vertexSource, string fragmentSource, string[] samplers)
    {
        var (resource, handle, uniformTable) = _resourceFactory.CreateShader(vertexSource, fragmentSource, samplers);
        Gl.UseProgram(handle);
        for (int i = 0; i < samplers.Length; i++)
            Gl.Uniform1(uniformTable.GetUniformLocation(samplers[i]), i);
        Gl.UseProgram(0);
        return _store.AddShaderResource(resource, uniformTable);
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


    public RenderPassDesc CreateFrameBuffer(in CreateRenderPassDesc desc)
    {
        var size = desc.Size ?? Ctx.ViewportSize;
        var result = _resourceFactory.CreateFrameBuffer(this, size);

        var texture = new GlTexture2D(result.Texture, size.X, size.Y, EnginePixelFormat.Rgba);
        var textureId = _store.AddResource(texture);

        var fbo = new GlFramebuffer( result.Fbo, result.Renderbuffer, textureId, size);
        var fboId = _store.AddResource(fbo);
        return new RenderPassDesc(
            Target: desc.Target,
            Order: desc.Order,
            FboId: fboId,
            Size: desc.Size.Value,
            Clear: true,
            ClearColor: desc.ClearColor,
            ClearMask: desc.ClearMask,
            ResolveTo: desc.ResolveTo,
            ResolveToFboId: desc.ResolveToFboId
        );
    }

    public void RemoveResource(ushort resourceId)
    {
        var resource = _store.Get(resourceId);

        _store.EnqueueRemoveResource(resourceId);
        if (resource is GlMesh mesh)
        {
            _store.EnqueueRemoveResource(mesh.VertexBufferId);
            if (mesh.IndexBufferId > 0)
                _store.EnqueueRemoveResource(mesh.IndexBufferId);
        }
        else if (resource is GlFramebuffer framebuffer)
        {
            _store.EnqueueRemoveResource(framebuffer.ColorTextureId);
        }
    }

    public void Dispose()
    {
        Console.WriteLine($"{nameof(GlGraphicsDevice)} Disposing {nameof(GlGraphicsDevice)} with {_store.Count} resources");

        int counter = 0;
        for (ushort i = 1; i <= _store.Count; i++)
        {
            var resource = _store.Get(i);
            if (resource == null || resource.IsDisposed) continue;
            FreeResource(resource);
            counter++;
        }
        
        Console.WriteLine($"{nameof(GlGraphicsDevice)} Disposed a total of {counter} resources");

        Gl.Dispose();
    }

    private void FreeResource(IGraphicsResource resource)
    {
        if (resource.IsDisposed) return;
        resource.IsDisposed = true;
        switch (resource)
        {
            case GlShader shader:
                Gl.DeleteShader(shader.Handle);
                break;
            case GlTexture2D texture:
                Gl.DeleteTexture(texture.Handle);
                break;
            case GlBuffer buffer:
                Gl.DeleteBuffer(buffer.Handle);
                break;
            case GlRenderTarget renderTarget:
                Gl.DeleteFramebuffer(renderTarget.Handle);
                break;
            case GlMesh mesh:
                Gl.DeleteVertexArray(mesh.Handle);
                break;
            case GlFramebuffer fbo:
                Gl.DeleteRenderbuffer(fbo.RenderBufferHandle);
                Gl.DeleteFramebuffer(fbo.Handle);
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
}