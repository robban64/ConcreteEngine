#region

using ConcreteEngine.Engine.Time.Tickers;

#endregion

namespace ConcreteEngine.Engine.Time;

internal sealed class EngineTimeHub(
    UpdateTickDelegate onGameTick,
    UpdateTickDelegate onSimulationTick,
    UpdateTickDelegate onLogTick)
{
    
    private readonly GameTickScheduler _gameTickScheduler = new(onGameTick, onSimulationTick, onLogTick);
    public DebounceTicker? DebounceTicker { get; set; } = null;


    public void AdvanceTick(float dt) => _gameTickScheduler.Advance(dt);
}