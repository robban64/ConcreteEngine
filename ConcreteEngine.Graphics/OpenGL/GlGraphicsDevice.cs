

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlGraphicsDevice : IGraphicsDevice<GlGraphicsContext>
{
    private readonly GL _gl;

    private readonly GlResourceFactory _resourceFactory;

    private readonly GraphicsResourceStore _store;

    public GL Gl => _gl;

    public GlGraphicsContext Ctx { get; }
    IGraphicsContext IGraphicsDevice.Ctx => Ctx;

    public GraphicsConfiguration Configuration { get; }
    public GraphicsBackend BackendApi => GraphicsBackend.OpenGL;

    public GlGraphicsDevice(GL gl, in RenderFrameContext initialFrameCtx)
    {
        _gl = gl;
        Configuration = new GraphicsConfiguration(CreateDeviceCapabilities(gl));
        _store = new GraphicsResourceStore(FreeResource);
        Ctx = new GlGraphicsContext(gl, Configuration, _store, in initialFrameCtx);
        Console.WriteLine("Device Capability");
        Console.WriteLine(Configuration.Capabilities.ToString());

        _resourceFactory = new GlResourceFactory(Ctx);
    }

    public void StartFrame(in RenderFrameContext frameCtx)
    {
        Ctx.Begin(in frameCtx);
    }

    public void StartDraw()
    {
        Ctx.BeginRender();
    }

    public void EndFrame()
    {
        Ctx.End();
        _store.FlushRemoveQueue();
    }

    public ushort CreateShader(string vertexSource, string fragmentSource)
    {
        var (resource, handle, uniformTable) = _resourceFactory.CreateShader(vertexSource, fragmentSource);
        return _store.AddShaderResource(resource, uniformTable);
    }

    public ushort CreateTexture2D(in TextureDescriptor textureDescriptor)
    {
        var resource = _resourceFactory.CreateTexture2D(in textureDescriptor);
        return _store.AddResource(resource);
    }
    public CreateMeshResult CreateMesh<T>(MeshDescriptor<T> meshData) where T : unmanaged
    {
        var resource = _resourceFactory.CreateMesh(this, meshData);
        var meshId = _store.AddResource(resource);
        return new CreateMeshResult(meshId, resource.VertexBufferId, resource.IndexBufferId, resource.DrawCount);
    }

    public void RemoveResource(ushort resourceId)
    {
        _store.EnqueueRemoveResource(resourceId);
    }

    public ushort CreateBuffer(BufferTarget target, BufferUsage usage)
    {
        var resource = _resourceFactory.CreateBuffer(target, usage);
        return _store.AddResource(resource);
    }
    public ushort CreateVertexBuffer(BufferUsage bufferUsage) => CreateBuffer(BufferTarget.VertexBuffer, bufferUsage);
    public ushort CreateIndexBuffer(BufferUsage bufferUsage) => CreateBuffer(BufferTarget.IndexBuffer, bufferUsage);


    public void Dispose()
    {
        Console.WriteLine($"Disposing {nameof(GlGraphicsDevice)} with {_store.Count} resources");
        
        for (ushort i = 1; i <= _store.Count; i++)
        {
            var resource = _store.Get(i);
            if(resource == null || resource.IsDisposed) continue;
            FreeResource(resource);
        }

        Gl.Dispose();
    }



    private void FreeResource(IGraphicsResource resource)
    {
        if(resource.IsDisposed) return;
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
                if (mesh.VertexBufferId != null && _store.TryGet<GlVertexBuffer>(mesh.VertexBufferId, out var vBuffer)) 
                    FreeResource(vBuffer);
                if (mesh.IndexBufferId != null && _store.TryGet<GlIndexBuffer>(mesh.IndexBufferId, out var iBuffer)) 
                    FreeResource(iBuffer);
                break;
        }
    }

    private static DeviceCapabilities CreateDeviceCapabilities(GL gl)
    {
        return new DeviceCapabilities
        {
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