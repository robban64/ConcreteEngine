namespace ConcreteEngine.Graphics.Gfx;

internal readonly struct DeleteResourceCommand(NativeHandle handle, ushort gfxId, GraphicsKind kind, bool replace)
    : IEquatable<DeleteResourceCommand>
{
    public readonly NativeHandle Handle = handle;
    public readonly ushort GfxId = gfxId;
    public readonly bool Replace = replace;
    public readonly GraphicsKind Kind = kind;

    public bool Equals(DeleteResourceCommand other) => Handle.Equals(other.Handle);
    public override bool Equals(object? obj) => obj is DeleteResourceCommand other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Handle.Value, GfxId, (byte)Kind);
}