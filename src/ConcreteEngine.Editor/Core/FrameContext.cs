namespace ConcreteEngine.Editor.Core;

internal readonly ref struct FrameContext
{
    public readonly Span<char> Buffer;
    public readonly float DeltaTime;
}