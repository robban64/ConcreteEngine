#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Rendering;
using ConcreteEngine.Graphics.Rendering.Sprite;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlGraphicsDevice : IGraphicsDevice<GlGraphicsContext>
{
    private static int resourceCounter = 0;

    private readonly GlResourceFactory _resourceFactory;

    private readonly IGraphicsResource[] _resources = new IGraphicsResource[128];

    private readonly GraphicsResourceStore _store;

    private readonly List<int> _resourceDisposeQueue = [];

    public GL Gl { get; }
    public GlGraphicsContext Ctx { get; }
    IGraphicsContext IGraphicsDevice.Ctx => Ctx;

    public GraphicsConfiguration Configuration { get; }
    public GraphicsBackend BackendApi => GraphicsBackend.OpenGL;
    public RenderPipeline RenderPipeline { get; }
    public SpriteBatchController SpriteBatchController { get; }

    public GlGraphicsDevice(GL gl, in RenderFrameContext initialFrameCtx)
    {
        Gl = gl;
        Configuration = new GraphicsConfiguration(CreateDeviceCapabilities(gl));
        _store = new GraphicsResourceStore(FreeResource);
        Ctx = new GlGraphicsContext(gl, Configuration, _store, in initialFrameCtx);
        Console.WriteLine("Device Capability");
        Console.WriteLine(Configuration.Capabilities.ToString());

        _resourceFactory = new GlResourceFactory(Ctx);
        RenderPipeline = new RenderPipeline(Ctx);
        SpriteBatchController = new SpriteBatchController(this);
    }

    public void StartFrame(in RenderFrameContext frameCtx)
    {
        Ctx.Begin(in frameCtx);
        SpriteBatchController.Prepare();
    }

    public void EndFrame()
    {
        Ctx.BeginRender();
        RenderPipeline.Execute();
        Ctx.End();

        _store.FlushRemoveQueue();
    }

    public int CreateShader(string vertexSource, string fragmentSource)
    {
        var resource = _resourceFactory.CreateShader(vertexSource, fragmentSource);
        return _store.AddResource(resource);
    }

    public int CreateTexture2D(in TextureDescriptor textureDescriptor)
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

    public void RemoveResource(int resourceId)
    {
        _store.EnqueueRemoveResource(resourceId);
    }

    public int CreateBuffer(BufferTarget target, BufferUsage usage)
    {
        var resource = _resourceFactory.CreateBuffer(target, usage);
        return _store.AddResource(resource);
    }
    public int CreateVertexBuffer(BufferUsage bufferUsage) => CreateBuffer(BufferTarget.VertexBuffer, bufferUsage);
    public int CreateIndexBuffer(BufferUsage bufferUsage) => CreateBuffer(BufferTarget.IndexBuffer, bufferUsage);


    public void Dispose()
    {
        Console.WriteLine($"Disposing {nameof(GlGraphicsDevice)} with {resourceCounter} resources");
        for (int i = 0; i < resourceCounter; i++)
        {
            var resource = _resources[i];
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
                //if (mesh.VertexBuffer != null) FreeResource(mesh.VertexBuffer);
                //if (mesh.IndexBuffer != null) FreeResource(mesh.IndexBuffer);
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