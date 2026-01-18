using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal ref struct FrameContext(SpanWriter sw, StateContext stateCtx, float deltaTime)
{
    public SpanWriter Sw = sw;
    public readonly StateContext StateCtx = stateCtx;
    public readonly float DeltaTime = deltaTime;
}