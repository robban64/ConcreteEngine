using ConcreteEngine.Core.Specs.Graphics;
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
    public readonly BackendResourceStore<GlTextureHandle> TextureStore = new(LargeCapacity);
    public readonly BackendResourceStore<GlShaderHandle> ShaderStore = new(MediumCapacity);
    public readonly BackendResourceStore<GlMeshHandle> MeshStore = new(LargeCapacity);
    public readonly BackendResourceStore<GlVboHandle> VboStore = new(LargeCapacity);
    public readonly BackendResourceStore<GlIboHandle> IboStore = new(LargeCapacity);
    public readonly BackendResourceStore<GlFboHandle> FboStore = new(LowCapacity);
    public readonly BackendResourceStore<GlRboHandle> RboStore = new(LowCapacity);
    public readonly BackendResourceStore<GlUboHandle> UboStore = new(LowCapacity);


    public BackendStoreHub()
    {
    }
    
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
    public BackendResourceStore<THandle> GetStore<THandle>()
         where THandle : unmanaged, IResourceHandle
    {
        var store = GetStore(THandle.Kind);
        if (store is not BackendResourceStore<THandle> typedStore)
            throw new InvalidOperationException("Missing backend store.");

        return typedStore;
    }

}