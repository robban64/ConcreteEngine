using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Handles;

public interface IResourceId
{
    int Value { get; }
    static abstract GraphicsKind Kind { get; }
}

public readonly record struct TextureId(ushort Id) : IResourceId
{
    public TextureId(int id) : this((ushort)id){}
    
    public readonly ushort Id = Id;
    public int Value => Id;
    public static GraphicsKind Kind => GraphicsKind.Texture;
    public static implicit operator int(TextureId id) => id.Id;
    public static explicit operator TextureId(int value) => new(value);
}

public readonly record struct ShaderId(ushort Id) : IResourceId
{
    public ShaderId(int id) : this((ushort)id){}
    
    public readonly ushort Id = Id;
    public int Value => Id;

    public static GraphicsKind Kind => GraphicsKind.Shader;
    public static implicit operator int(ShaderId id) => id.Id;
    public static explicit operator ShaderId(int value) => new(value);
}

public readonly record struct MeshId(ushort Id) : IResourceId
{
    public MeshId(int id) : this((ushort)id){}
    
    public readonly ushort Id = Id;
    public int Value => Id;

    public static GraphicsKind Kind => GraphicsKind.Mesh;
    public static implicit operator int(MeshId id) => id.Id;
    public static explicit operator MeshId(int value) => new(value);
}

public readonly record struct VertexBufferId(ushort Id) : IResourceId
{
    public VertexBufferId(int id) : this((ushort)id){}
    
    public readonly ushort Id = Id;
    public int Value => Id;

    public static GraphicsKind Kind => GraphicsKind.VertexBuffer;
    public static implicit operator int(VertexBufferId id) => id.Id;
    public static explicit operator VertexBufferId(int value) => new(value);
}

public readonly record struct IndexBufferId(ushort Id) : IResourceId
{
    public IndexBufferId(int id) : this((ushort)id){}
    
    public readonly ushort Id = Id;
    public int Value => Id;

    public static GraphicsKind Kind => GraphicsKind.IndexBuffer;
    public static implicit operator int(IndexBufferId id) => id.Id;
    public static explicit operator IndexBufferId(int value) => new(value);
}

public readonly record struct FrameBufferId(ushort Id) : IResourceId
{
    public FrameBufferId(int id) : this((ushort)id){}
    
    public readonly ushort Id = Id;
    public int Value => Id;

    public static GraphicsKind Kind => GraphicsKind.FrameBuffer;
    public static implicit operator int(FrameBufferId id) => id.Id;
    public static explicit operator FrameBufferId(int value) => new(value);
}

public readonly record struct RenderBufferId(ushort Id) : IResourceId
{
    public RenderBufferId(int id) : this((ushort)id){}
    
    public readonly ushort Id = Id;
    public int Value => Id;

    public static GraphicsKind Kind => GraphicsKind.RenderBuffer;
    public static implicit operator int(RenderBufferId id) => id.Id;
    public static explicit operator RenderBufferId(int value) => new(value);
}

public readonly record struct UniformBufferId(ushort Id) : IResourceId
{
    public UniformBufferId(int id) : this((ushort)id){}
    
    public readonly ushort Id = Id;
    public int Value => Id;

    public static GraphicsKind Kind => GraphicsKind.UniformBuffer;
    public static implicit operator int(UniformBufferId id) => id.Id;
    public static explicit operator UniformBufferId(int value) => new(value);
}

public static class ResourceIdExtensions
{
    extension<T>(T t) where T : unmanaged, IResourceId
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => t.Value > 0;
    }
}