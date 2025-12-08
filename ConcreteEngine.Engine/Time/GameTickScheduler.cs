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

    private float _alpha;
    private float _speed = 1f;


    internal GameTickScheduler(UpdateTickDelegate onGameTick, UpdateTickDelegate onSimulationTick, UpdateTickDelegate onLogTick)
    {
        _onGameTick = onGameTick;
        _onSimulationTick = onSimulationTick;
        _onLogTick = onLogTick;
    }

    public float Alpha => _alpha;
    public float FixedDeltaTime => GameTickDeltaTime;

    public void Advance(float deltaTime)
    {
        float dt = deltaTime * _speed;

        _gameTicker.Accumulate(dt);
        _simulationTicker.Accumulate(dt);
        _diagnosticTicker.Accumulate(dt);

        int t, tickCounter = 0;
        while (tickCounter < MaxTicksPerFrame && _gameTicker.TryDequeueTick(out t))
        {
            _onGameTick(new UpdateTickArgs(t, GameTickDeltaTime, _gameTicker.Alpha));
            tickCounter++;
        }

        while (_simulationTicker.TryDequeueTick(out var t1))
        {
            _onSimulationTick(new UpdateTickArgs(t1, SimulationDeltaTime, _simulationTicker.Alpha));
        }

        while (_diagnosticTicker.TryDequeueTick(out var t2))
        {
            _onLogTick(new UpdateTickArgs(t2, DiagnosticTickDeltaTime, _diagnosticTicker.Alpha));
        }

        _alpha = _gameTicker.Alpha;
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