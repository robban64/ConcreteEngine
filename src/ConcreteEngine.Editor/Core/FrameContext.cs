using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ZaString.Core;

namespace ConcreteEngine.Editor.Core;

internal ref struct FrameContext(SpanWriter writer, float deltaTime, ModeState mode)
{
    public SpanWriter Writer = writer;
    public readonly float DeltaTime = deltaTime;
    public readonly ModeState Mode = mode;
}