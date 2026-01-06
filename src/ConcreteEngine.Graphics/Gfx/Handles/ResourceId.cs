using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Gfx.Handles;

public interface IResourceId
{
    int Value { get; }
    static abstract GraphicsKind Kind { get; }
}

public readonly record struct TextureId(int Value) : IResourceId
{
    public static GraphicsKind Kind => GraphicsKind.Texture;
    public static implicit operator int(TextureId id) => id.Value;
    public static explicit operator TextureId(int value) => new(value);
}

public readonly record struct ShaderId(int Value) : IResourceId
{
    public static GraphicsKind Kind => GraphicsKind.Shader;
    public static implicit operator int(ShaderId id) => id.Value;
    public static explicit operator ShaderId(int value) => new(value);
}

public readonly record struct MeshId(int Value) : IResourceId
{
    public static GraphicsKind Kind => GraphicsKind.Mesh;
    public static implicit operator int(MeshId id) => id.Value;
    public static explicit operator MeshId(int value) => new(value);
}

public readonly record struct VertexBufferId(int Value) : IResourceId
{
    public static GraphicsKind Kind => GraphicsKind.VertexBuffer;
    public static implicit operator int(VertexBufferId id) => id.Value;
    public static explicit operator VertexBufferId(int value) => new(value);
}

public readonly record struct IndexBufferId(int Value) : IResourceId
{
    public static GraphicsKind Kind => GraphicsKind.IndexBuffer;
    public static implicit operator int(IndexBufferId id) => id.Value;
    public static explicit operator IndexBufferId(int value) => new(value);
}

public readonly record struct FrameBufferId(int Value) : IResourceId
{
    public static GraphicsKind Kind => GraphicsKind.FrameBuffer;
    public static implicit operator int(FrameBufferId id) => id.Value;
    public static explicit operator FrameBufferId(int value) => new(value);
}

public readonly record struct RenderBufferId(int Value) : IResourceId
{
    public static GraphicsKind Kind => GraphicsKind.RenderBuffer;
    public static implicit operator int(RenderBufferId id) => id.Value;
    public static explicit operator RenderBufferId(int value) => new(value);
}

public readonly record struct UniformBufferId(int Value) : IResourceId
{
    public static GraphicsKind Kind => GraphicsKind.UniformBuffer;
    public static implicit operator int(UniformBufferId id) => id.Value;
    public static explicit operator UniformBufferId(int value) => new(value);
}

public static class ResourceIdExtensions
{
    extension<T>(T t) where T : unmanaged, IResourceId
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => t.Value > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugValidate()
        {
            Debug.Assert(IsValid(t), $"ResourceId {t.Value} is not valid");
        }

        public void IsValidOrThrow()
        {
            if (!IsValid(t))
                throw new GraphicsException($"ResourceId {t.Value} is not valid");
        }
    }
}