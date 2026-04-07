using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.Configuration;

public static class PathUtils
{
    public const int MaxPathLength = 260;
    public const int JoinPathLength = MaxPathLength * 2;

    public static unsafe NativeView<byte> JoinPath(char* buffer, string p1, string p2, string? p3 = null)
    {
        var ptr = new NativeView<char>(buffer, JoinPathLength);
        var chars = ptr.Slice(0, MaxPathLength).AsSpan();
        var bytes = ptr.SliceFrom(MaxPathLength).Reinterpret<byte>();

        var result = p3 == null
            ? Path.TryJoin(p1, p2, chars, out var written)
            : Path.TryJoin(p1, p2, p3, chars, out written);

        if (!result) throw new InvalidOperationException("Path could not be joined");

        return bytes.Writer().Append(chars.Slice(0, written)).End();
    }
}