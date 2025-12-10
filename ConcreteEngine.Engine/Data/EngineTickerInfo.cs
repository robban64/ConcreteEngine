#region

using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Engine.Data;

public readonly record struct UpdateTickInfo(long UpdateIndex, float DeltaTime);

public sealed class EngineTickerInfo
{
    private UpdateTickInfo _updateTickInfo;

    public ref readonly UpdateTickInfo UpdateTickInfo => ref _updateTickInfo;

    internal void BeginUpdateFrame(float deltaTime, Size2D viewport)
    {
        _updateTickInfo = new UpdateTickInfo(UpdateIndex: _updateTickInfo.UpdateIndex + 1, DeltaTime: deltaTime);
    }
}