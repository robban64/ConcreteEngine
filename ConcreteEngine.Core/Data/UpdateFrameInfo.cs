#region

using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Core.Data;

public readonly record struct UpdateTickInfo(
    long FrameIndex,
    int GameTick,
    float DeltaTime)
{
    public float Fps => DeltaTime > 0 ? 1.0f / DeltaTime : 0.0f;
}

public sealed class UpdateFrameInfo
{
    private UpdateTickInfo _updateTickInfo;
    
    public ref readonly UpdateTickInfo UpdateTickInfo => ref _updateTickInfo;

    internal void BeginUpdateFrame(float deltaTime, Size2D viewport, Size2D outputSize)
    {
        _updateTickInfo = _updateTickInfo with
        {
            FrameIndex = _updateTickInfo.FrameIndex + 1,
            DeltaTime = deltaTime,
        };
    }

    internal void UpdateTick(int tick)
    {
        _updateTickInfo = _updateTickInfo with { GameTick = tick };
    }
}