#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;

#endregion

namespace ConcreteEngine.Graphics.Resources;


public readonly struct ResourceIdComparer<TId> : IComparer<TId> where TId : unmanaged,IResourceId 
{
    public int Compare(TId x, TId y)
    {
        return x.Value.CompareTo(y.Value);
    }
}

public interface IResourceId
{
    int Value { get; }
    static abstract ResourceKind Kind { get; }
}


public readonly record struct TextureId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.Texture;
}

public readonly record struct ShaderId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.Shader;
}

public readonly record struct MeshId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.Mesh;
}

public readonly record struct VertexBufferId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.VertexBuffer;
}

public readonly record struct IndexBufferId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.IndexBuffer;
}

public readonly record struct FrameBufferId(int Value) : IResourceId
{
    public static ResourceKind Kind =>  ResourceKind.FrameBuffer;
}

public readonly record struct RenderBufferId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.FrameBuffer;
}

public readonly record struct UniformBufferId(int Value) : IResourceId
{
    public static ResourceKind Kind => ResourceKind.UniformBuffer;
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