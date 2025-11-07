#region

using ConcreteEngine.Engine.Data;
using ConcreteEngine.Engine.Time.Tickers;

#endregion

namespace ConcreteEngine.Engine.Time;

internal sealed class EngineTimeHub
{
    private readonly GameTickScheduler _gameTickScheduler;
    public DebounceTicker? DebounceTicker { get; set; } = null;
    public RenderTickScheduler RenderTicker { get; } = new();

    public float Alpha => _gameTickScheduler.Alpha;

    public EngineTimeHub(UpdateTickDelegate onGameTick, UpdateTickDelegate onUpdateLogTick)
    {
        _gameTickScheduler = new GameTickScheduler(onGameTick, onUpdateLogTick);
    }

    public void AdvanceTick(float dt) => _gameTickScheduler.Advance(dt);
}