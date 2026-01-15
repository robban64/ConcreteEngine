using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ZaString.Core;

namespace ConcreteEngine.Editor.Core;

internal readonly ref struct FrameContext(Span<byte> buffer, float deltaTime, ModeState mode)
{
    public readonly Span<byte> Buffer = buffer;
    public readonly float DeltaTime = deltaTime;
    public readonly ModeState Mode = mode;

    public ZaUtf8SpanWriter GetWriter() => ZaUtf8SpanWriter.Create(Buffer);
}