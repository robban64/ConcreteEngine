using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.UI;

internal static class WriteFormat
{
    public static ReadOnlySpan<byte> WriteSize(SpanWriter sw, Size2D size) =>
        sw.Start(size.Width).Append("x"u8).Append(size.Height).End();

    public static ReadOnlySpan<byte> WriteTitleId(SpanWriter sw, ReadOnlySpan<byte> subject, int id) =>
        sw.Start(subject).Append(" ["u8).Append(id).Append("]"u8).End();

    public static ReadOnlySpan<byte> WriteIdAndGen(SpanWriter sw, int id, int gen) =>
        sw.Start(" ["u8).Append(id).Append(":"u8).Append(gen).Append("]"u8).End();
}