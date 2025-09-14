using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class OpenGlResourceStores : IDriverResourceStoreCollection
{
    private const GraphicsBackend Backend = GraphicsBackend.OpenGL;
    public readonly DriverResourceStore<GlTextureHandle> TextureStore = new(Backend, ResourceKind.Texture);
    public readonly DriverResourceStore<GlShaderHandle> ShaderStore = new(Backend, ResourceKind.Shader);
    public readonly DriverResourceStore<GlMeshHandle> MeshStore = new(Backend, ResourceKind.Mesh);
    public readonly DriverResourceStore<GlVboHandle> VboStore = new(Backend, ResourceKind.VertexBuffer);
    public readonly DriverResourceStore<GlIboHandle> IboStore = new(Backend, ResourceKind.IndexBuffer);
    public readonly DriverResourceStore<GlFboHandle> FboStore = new(Backend, ResourceKind.FrameBuffer);
    public readonly DriverResourceStore<GlRboHandle> RboStore = new(Backend, ResourceKind.RenderBuffer);
    public readonly DriverResourceStore<GlUboHandle> UboStore = new(Backend, ResourceKind.UniformBuffer);

    public OpenGlResourceStores(BackendStoreHub storeHub)
    {
        storeHub.RegisterStore(ResourceKind.Texture, TextureStore);
        storeHub.RegisterStore(ResourceKind.Shader, ShaderStore);
        storeHub.RegisterStore(ResourceKind.Mesh, MeshStore);
        storeHub.RegisterStore(ResourceKind.VertexBuffer, VboStore);
        storeHub.RegisterStore(ResourceKind.IndexBuffer, IboStore);
        storeHub.RegisterStore(ResourceKind.FrameBuffer, FboStore);
        storeHub.RegisterStore(ResourceKind.RenderBuffer, RboStore);
        storeHub.RegisterStore(ResourceKind.UniformBuffer, UboStore);
    }
}