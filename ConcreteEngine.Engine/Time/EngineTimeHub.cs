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


    public EngineTimeHub(UpdateTickDelegate onGameTick, UpdateTickDelegate onSimulationTick,
        UpdateTickDelegate onUpdateLogTick)
    {
        _gameTickScheduler = new GameTickScheduler(onGameTick, onSimulationTick, onUpdateLogTick);
    }

    public float Alpha => _gameTickScheduler.Alpha;
    public float FixedDeltaTime => _gameTickScheduler.FixedDeltaTime;


    public void AdvanceTick(float dt) => _gameTickScheduler.Advance(dt);
}