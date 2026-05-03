using ConcreteEngine.Graphics.Gfx.Data;
using ConcreteEngine.Graphics.Gfx.Handles;
using static ConcreteEngine.Graphics.Configuration.GfxLimits;

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal sealed class ResourceBackendDispatcher
{
    public required Action<DeleteResourceCommand> OnDelete { get; init; }
}

internal sealed class BackendStoreHub
{
    public readonly BackendResourceStore<GlHandle> TextureStore = new(LargeCapacity, GraphicsKind.Texture);
    public readonly BackendResourceStore<GlHandle> ShaderStore = new(MediumCapacity, GraphicsKind.Shader);
    public readonly BackendResourceStore<GlHandle> MeshStore = new(LargeCapacity, GraphicsKind.Mesh);
    public readonly BackendResourceStore<GlHandle> VboStore = new(LargeCapacity, GraphicsKind.VertexBuffer);
    public readonly BackendResourceStore<GlHandle> IboStore = new(LargeCapacity,GraphicsKind.IndexBuffer);
    public readonly BackendResourceStore<GlHandle> FboStore = new(LowCapacity, GraphicsKind.FrameBuffer);
    public readonly BackendResourceStore<GlHandle> RboStore = new(LowCapacity,GraphicsKind.RenderBuffer);
    public readonly BackendResourceStore<GlHandle> UboStore = new(LowCapacity, GraphicsKind.UniformBuffer);

    public IBackendResourceStore GetStore(GraphicsKind kind)
    {
        switch (kind)
        {
            case GraphicsKind.Texture: return TextureStore;
            case GraphicsKind.Shader: return ShaderStore;
            case GraphicsKind.Mesh: return MeshStore;
            case GraphicsKind.VertexBuffer: return VboStore;
            case GraphicsKind.IndexBuffer: return IboStore;
            case GraphicsKind.FrameBuffer: return FboStore;
            case GraphicsKind.RenderBuffer: return RboStore;
            case GraphicsKind.UniformBuffer: return UboStore;
            case GraphicsKind.Invalid:
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Invalid resource kind.");
        }
    }
}