using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class OpenGlResourceStores
{
    public readonly DriverResourceStore<GlTextureHandle> TextureStore = new(GraphicsBackend.OpenGL, ResourceKind.Texture);
    public readonly DriverResourceStore<GlShaderHandle> ShaderStore = new(GraphicsBackend.OpenGL, ResourceKind.Shader);
    public readonly DriverResourceStore<GlMeshHandle> MeshStore = new(GraphicsBackend.OpenGL, ResourceKind.Mesh);
    public readonly DriverResourceStore<GlVboHandle> VboStore = new(GraphicsBackend.OpenGL, ResourceKind.VertexBuffer);
    public readonly DriverResourceStore<GlIboHandle> IboStore = new(GraphicsBackend.OpenGL, ResourceKind.IndexBuffer);
    public readonly DriverResourceStore<GlFboHandle> FboStore = new(GraphicsBackend.OpenGL, ResourceKind.FrameBuffer);
    public readonly DriverResourceStore<GlRboHandle> RboStore = new(GraphicsBackend.OpenGL, ResourceKind.RenderBuffer);
    public readonly DriverResourceStore<GlUboHandle> UboStore = new(GraphicsBackend.OpenGL, ResourceKind.UniformBuffer);

}