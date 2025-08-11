#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Rendering;
using ConcreteEngine.Graphics.Rendering.Sprite;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlGraphicsDevice : IGraphicsDevice<GlGraphicsContext>
{
    private readonly GlResourceFactory _resourceFactory;

    private readonly List<IGraphicsResource> _resources = [];
    private readonly HashSet<IGraphicsResource> _resourceDisposeQueue = [];

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
        Ctx = new GlGraphicsContext(gl, Configuration, in initialFrameCtx);
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

        if (_resourceDisposeQueue.Count > 0)
        {
            foreach (var resource in _resourceDisposeQueue)
            {
                FreeResource(resource);
            }

            _resourceDisposeQueue.Clear();
        }
    }

    public IShader CreateShader(string vertexSource, string fragmentSource)
    {
        var resource = _resourceFactory.CreateShader(vertexSource, fragmentSource);
        _resources.Add(resource);
        return resource;
    }

    public ITexture2D CreateTexture2D(in TextureDescriptor textureDescriptor)
    {
        var resource = _resourceFactory.CreateTexture2D(in textureDescriptor);
        _resources.Add(resource);
        return resource;
    }

    public GlVertexBuffer CreateVertexBuffer(BufferUsage bufferUsage) =>
        (GlVertexBuffer)CreateBuffer(BufferTarget.VertexBuffer, bufferUsage);

    public GlIndexBuffer CreateIndexBuffer(BufferUsage bufferUsage) =>
        (GlIndexBuffer)CreateBuffer(BufferTarget.IndexBuffer, bufferUsage);

    public IGraphicsBuffer CreateBuffer(BufferTarget target, BufferUsage usage)
    {
        var buffer = _resourceFactory.CreateBuffer(target, usage);
        _resources.Add(buffer);
        return buffer;
    }

    public IMesh CreateMesh<T>(MeshDescriptor<T> meshData) where T : unmanaged
    {
        var mesh = _resourceFactory.CreateMesh(this, meshData);
        _resources.Add(mesh);
        return mesh;
    }

    public void RemoveResource<TResource>(TResource resource) where TResource : IGraphicsResource
    {
        if (resource.IsDisposed) throw new ObjectDisposedException(nameof(GlGraphicsDevice));
        _resourceDisposeQueue.Add(resource);
    }

    public void Dispose()
    {
        Console.WriteLine($"Disposing {nameof(GlGraphicsDevice)} with {_resources.Count} resources");
        _resources.ForEach(FreeResource);
        Gl.Dispose();
    }

    private void FreeResource<TResource>(TResource resource) where TResource : IGraphicsResource
    {
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
                if (mesh.VertexBuffer != null) FreeResource(mesh.VertexBuffer);
                if (mesh.IndexBuffer != null) FreeResource(mesh.IndexBuffer);
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