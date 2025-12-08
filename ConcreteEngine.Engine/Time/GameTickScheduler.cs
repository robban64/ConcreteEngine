#region

using ConcreteEngine.Engine.Time.Tickers;

#endregion

namespace ConcreteEngine.Engine.Time;

//https://gafferongames.com/post/fix_your_timestep/
internal sealed class GameTickScheduler
{
    private const int MaxTicksPerFrame = 6;
    private const int GameTicksPerSecond = 60;
    private const int SimulationTickPerSecond = 20;


    private const float GameTickDeltaTime = 1f / GameTicksPerSecond;
    private const float SimulationDeltaTime = 1f / SimulationTickPerSecond;

    private const float DiagnosticTickDeltaTime = 1f / 4;

    private readonly FrameTickTimer _gameTicker = new(GameTickDeltaTime);
    private readonly FrameTickTimer _simulationTicker = new(SimulationDeltaTime);
    private readonly FrameTickTimer _diagnosticTicker = new(DiagnosticTickDeltaTime);

    private SimpleTicker _animationClock = new(5, GameTicksPerSecond);

    private readonly UpdateTickDelegate _onGameTick;
    private readonly UpdateTickDelegate _onSimulationTick;
    private readonly UpdateTickDelegate _onLogTick;


    internal GameTickScheduler(UpdateTickDelegate onGameTick, UpdateTickDelegate onSimulationTick,
        UpdateTickDelegate onLogTick)
    {
        _onGameTick = onGameTick;
        _onSimulationTick = onSimulationTick;
        _onLogTick = onLogTick;
    }

    public void Advance(float deltaTime)
    {
        float dt = deltaTime;

        _gameTicker.Accumulate(dt);
        _simulationTicker.Accumulate(dt);
        _diagnosticTicker.Accumulate(dt);

        int t, t1, t2, tickCounter = 0;
        while (tickCounter < MaxTicksPerFrame && _gameTicker.TryDequeueTick(out t))
        {
            _onGameTick(_gameTicker.ToArgs());
            tickCounter++;
        }

        while (_simulationTicker.TryDequeueTick(out t1))
            _onSimulationTick(_simulationTicker.ToArgs());

        while (_diagnosticTicker.TryDequeueTick(out t2))
            _onLogTick(_diagnosticTicker.ToArgs());

        EngineTime.GameTime = _gameTicker.ToArgs();
        EngineTime.SimulationDeltaTime = _simulationTicker.ToArgs();
        EngineTime.DiagnosticDeltaTime = _diagnosticTicker.ToArgs();
    }

    private void GameTickUpdate(int tick)
    {
        _animationClock.AccumulateGameTime();
        if (_animationClock.TryDequeueTick(out var t))
        {
            //TODO implement animation handler
        }
    }
}