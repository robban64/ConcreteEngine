using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal ref struct FrameContext(SpanWriter sw, float deltaTime)
{
    public SpanWriter Sw = sw;
    public readonly float DeltaTime = deltaTime;
}