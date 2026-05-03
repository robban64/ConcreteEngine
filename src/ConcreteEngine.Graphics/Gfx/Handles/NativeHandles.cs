namespace ConcreteEngine.Graphics.Gfx.Handles;

internal interface IGraphicsHandle
{
    uint Value { get; }
}

internal readonly record struct GlHandle(uint Value) : IGraphicsHandle
{
    public static implicit operator GlHandle(BkHandle handle) => new (handle.Handle);
    public static implicit operator uint(GlHandle handle) => handle.Value;
    public static implicit operator NativeHandle(GlHandle handle) => new(handle.Value);
}

internal readonly record struct NativeHandle(nint Value)
{
    public NativeHandle(uint value) : this((nint)value) { }
    public static implicit operator nint(NativeHandle handle) => handle.Value;
    public static implicit operator uint(NativeHandle handle) => (uint)handle.Value;
}
