#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

public interface IResourceId
{
    int Value { get; }
    static abstract ResourceKind Kind { get; }
}

public readonly record struct TextureId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.Texture;
    public static implicit operator int(TextureId id) => id.Value;
    public static explicit operator TextureId(int value) => new(value);
}

public readonly record struct ShaderId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.Shader;
    public static implicit operator int(ShaderId id) => id.Value;
    public static explicit operator ShaderId(int value) => new(value);
}

public readonly record struct MeshId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.Mesh;
    public static implicit operator int(MeshId id) => id.Value;
    public static explicit operator MeshId(int value) => new(value);
}

public readonly record struct VertexBufferId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.VertexBuffer;
    public static implicit operator int(VertexBufferId id) => id.Value;
    public static explicit operator VertexBufferId(int value) => new(value);
}

public readonly record struct IndexBufferId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.IndexBuffer;
    public static implicit operator int(IndexBufferId id) => id.Value;
    public static explicit operator IndexBufferId(int value) => new(value);
}

public readonly record struct FrameBufferId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.FrameBuffer;
    public static implicit operator int(FrameBufferId id) => id.Value;
    public static explicit operator FrameBufferId(int value) => new(value);
}

public readonly record struct RenderBufferId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.FrameBuffer;
    public static implicit operator int(RenderBufferId id) => id.Value;
    public static explicit operator RenderBufferId(int value) => new(value);
}

public readonly record struct UniformBufferId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.UniformBuffer;
    public static implicit operator int(UniformBufferId id) => id.Value;
    public static explicit operator UniformBufferId(int value) => new(value);
}

public static class ResourceIdExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid<T>(this T t) where T : unmanaged, IResourceId => t.Value > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DebugValidate<T>(this T t) where T : unmanaged, IResourceId
    {
        Debug.Assert(IsValid(t), $"ResourceId {t.Value} is not valid");
    }

    public static void IsValidOrThrow<T>(this T t) where T : unmanaged, IResourceId
    {
        if (!IsValid(t))
            throw new GraphicsException($"ResourceId {t.Value} is not valid");
    }
}