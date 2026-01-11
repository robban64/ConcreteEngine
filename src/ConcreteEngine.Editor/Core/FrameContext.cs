using ConcreteEngine.Editor.Data;
using ZaString.Core;

namespace ConcreteEngine.Editor.Core;

internal readonly ref struct FrameContext(Span<byte> buffer, float deltaTime, float leftWidth, float rightWidth, ModeState mode)
{
    public readonly Span<byte> Buffer = buffer;
    public readonly float DeltaTime = deltaTime;
    public readonly float LeftWidth = leftWidth;
    public readonly float RightWidth = rightWidth;
    public readonly ModeState Mode = mode;

    public ZaUtf8SpanWriter GetWriter() => ZaUtf8SpanWriter.Create(Buffer);
}