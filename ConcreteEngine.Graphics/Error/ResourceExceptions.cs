using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Error;

internal static class ResourceExceptions
{
    public static void ThrowIfInvalidGfx(in GfxHandle handle)
    {
        if (!handle.IsValid) throw new ArgumentException($"Invalid GfxHandle: {handle}", nameof(handle));
    }

    public static void ThrowIfInvalidHandle<THandle>(in THandle handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (handle.Handle == 0) throw new ArgumentException($"Invalid {typeof(THandle).Name}: {handle}", nameof(handle));
    }
    
    public static void ThrowIfInvalidId<TId>(in TId handle) where TId : unmanaged, IResourceId
    {
        if (handle.Value == 0) throw new ArgumentException($"Invalid {typeof(TId).Name}: {handle}", nameof(handle));
    }

    public static void ThrowIfInvalid(in NativeHandle handle)
    {
        if (handle.Value == 0) throw new ArgumentException($"Invalid NativeHandle: {handle}", nameof(handle));
    }
}