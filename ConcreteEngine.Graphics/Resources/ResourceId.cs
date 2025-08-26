namespace ConcreteEngine.Graphics.Resources;

public interface IResourceId
{
    int Id { get; }
}

public readonly record struct TextureId(int Id) : IResourceId;

public readonly record struct ShaderId(int Id) : IResourceId;

public readonly record struct MeshId(int Id) : IResourceId;

public readonly record struct VertexBufferId(int Id) : IResourceId;

public readonly record struct IndexBufferId(int Id) : IResourceId;

public readonly record struct FrameBufferId(int Id) : IResourceId;

public readonly record struct RenderBufferId(int Id) : IResourceId;