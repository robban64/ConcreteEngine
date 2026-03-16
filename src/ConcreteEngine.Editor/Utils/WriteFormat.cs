using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal static unsafe class WriteFormat
{
    public static byte* WriteSize(UnsafeSpanWriter sw, Size2D size) =>
         sw.Append(size.Width).Append('x').Append(size.Height).End();

    public static ReadOnlySpan<byte> BoolToYesNoShort(bool value) => value ? "Y"u8 : "N"u8;
}