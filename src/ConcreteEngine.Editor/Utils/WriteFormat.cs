using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Editor.Utils;

internal static class WriteFormat
{
    public static ref byte WriteSize(StrWriter8 sw, Size2D size) =>
        ref sw.Start(size.Width).Append("x"u8).Append(size.Height).End();

    public static ref byte WriteTitleId(StrWriter8 sw, ReadOnlySpan<byte> subject, int id) =>
        ref sw.Start(subject).Append(" ["u8).Append(id).Append("]"u8).End();

    public static ref byte WriteIdAndGen(StrWriter8 sw, int id, int gen) =>
        ref sw.Start(" ["u8).Append(id).Append(":"u8).Append(gen).Append("]"u8).End();

    public static ReadOnlySpan<byte> BoolToYesNoShort(bool value) => value ? "Y"u8 : "N"u8;
}