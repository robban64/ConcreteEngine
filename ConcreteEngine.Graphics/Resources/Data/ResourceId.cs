using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;

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
public readonly record struct UniformBufferId(int Id) : IResourceId;


public static class ResourceIdExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid<T>(this T t) where T : struct, IResourceId
        => t.Id > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DebugValidate<T>(this T t) where T : struct, IResourceId
    {
        Debug.Assert(IsValid(t), $"ResourceId {t.Id} is not valid");
    }
    
    public static void IsValidOrThrow<T>(this T t) where T : struct, IResourceId
    {
        if (!IsValid(t))
            throw new GraphicsException($"ResourceId {t.Id} is not valid");
    }

}