#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;

#endregion

namespace ConcreteEngine.Graphics.Resources;

public interface IResourceId
{
    int Value { get; }
}

public readonly record struct TextureId(int Value) : IResourceId;

public readonly record struct ShaderId(int Value) : IResourceId;

public readonly record struct MeshId(int Value) : IResourceId;

public readonly record struct VertexBufferId(int Value) : IResourceId;

public readonly record struct IndexBufferId(int Value) : IResourceId;

public readonly record struct FrameBufferId(int Value) : IResourceId;

public readonly record struct RenderBufferId(int Value) : IResourceId;

public readonly record struct UniformBufferId(int Value) : IResourceId;

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