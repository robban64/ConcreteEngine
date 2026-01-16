using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal ref struct FrameContext(SpanWriter sw, float deltaTime, ModeState mode)
{
    public SpanWriter Sw = sw;
    public readonly float DeltaTime = deltaTime;
    public readonly ModeState Mode = mode;
}