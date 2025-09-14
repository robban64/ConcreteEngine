using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class ResourceStoreHub
{
    private const int StoreTier1 = 64;
    private const int StoreTier2 = 32;
    private const int StoreTier3 = 16;
    
    private Dictionary<ResourceKind, IResourceStore> _stores = new(8);
    private Dictionary<Type, IResourceStore> _storesTyped = new(8);

    public ResourceStoreHub()
    {
        RegisterStore(ResourceKind.Texture, TextureStore);
        RegisterStore(ResourceKind.Shader, ShaderStore);
        RegisterStore(ResourceKind.Mesh, MeshStore);
        RegisterStore(ResourceKind.VertexBuffer, VboStore);
        RegisterStore(ResourceKind.IndexBuffer, IboStore);
        RegisterStore(ResourceKind.FrameBuffer, FboStore);
        RegisterStore(ResourceKind.RenderBuffer, RboStore);
        RegisterStore(ResourceKind.UniformBuffer, UboStore);
    }

    public ResourceStore<TId, TMeta> GetStore<TId, TMeta>() 
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        var store = _storesTyped[typeof(TId)];
        if(store is ResourceStore<TId, TMeta> typedStore)
            return typedStore;
        
        throw new Exception($"Resource store doesn't support or is not registerd for type {typeof(TId).Name}");
    }
    
    private void RegisterStore<TId, TMeta>(ResourceKind key, ResourceStore<TId, TMeta> store) 
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        _stores.Add(key, store);
        _storesTyped.Add(typeof(TId), store);
    }

    public ResourceStore<TextureId, TextureMeta> TextureStore { get; } =
        new(ResourceKind.Texture,StoreTier1, static i => new TextureId(i + 1));

    public ResourceStore<ShaderId, ShaderMeta> ShaderStore { get; }
        = new(ResourceKind.Texture,StoreTier2, static i => new ShaderId(i + 1));

    public ResourceStore<MeshId, MeshMeta> MeshStore { get; }
        = new(ResourceKind.Texture,StoreTier2, static i => new MeshId(i + 1));

    public ResourceStore<VertexBufferId, VertexBufferMeta> VboStore { get; }
        = new(ResourceKind.Texture,StoreTier2, static i => new VertexBufferId(i + 1));

    public ResourceStore<IndexBufferId, IndexBufferMeta> IboStore { get; }
        = new(ResourceKind.Texture,StoreTier2, static i => new IndexBufferId(i + 1));

    public ResourceStore<FrameBufferId, FrameBufferMeta> FboStore { get; }
        = new(ResourceKind.Texture,StoreTier3, static i => new FrameBufferId(i + 1));

    public ResourceStore<RenderBufferId, RenderBufferMeta> RboStore { get; }
        = new(ResourceKind.Texture,StoreTier3, static i => new RenderBufferId(i + 1));

    public ResourceStore<UniformBufferId, UniformBufferMeta> UboStore { get; }
        = new(ResourceKind.Texture,StoreTier3, static i => new UniformBufferId(i + 1));
}