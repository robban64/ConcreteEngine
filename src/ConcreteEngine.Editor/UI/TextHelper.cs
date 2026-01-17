using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.UI;

internal static class TextHelper
{
    public static ReadOnlySpan<byte> WriteSize(ref SpanWriter sw, Size2D size) =>
        sw.Start(size.Width).Append("x"u8).Append(size.Height).End();
}