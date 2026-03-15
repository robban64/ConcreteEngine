using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal static class WriteFormat
{
    public static ref byte WriteSize(UnsafeSpanWriter sw, Size2D size) =>
        ref sw.Append(size.Width).Append('x').Append(size.Height).End();

    public static ref byte WriteTitleId(UnsafeSpanWriter sw, ReadOnlySpan<byte> subject, int id) =>
        ref sw.Append(subject).Append(" ["u8).Append(id).Append(']').End();

    public static ref byte WriteIdAndGen(UnsafeSpanWriter sw, int id, int gen) =>
        ref sw.Append(" ["u8).Append(id).Append(':').Append(gen).Append(']').End();

    public static ReadOnlySpan<byte> BoolToYesNoShort(bool value) => value ? "Y"u8 : "N"u8;
}