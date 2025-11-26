#region

using ConcreteEngine.Engine.Data;
using ConcreteEngine.Engine.Time.Tickers;

#endregion

namespace ConcreteEngine.Engine.Time;

//https://gafferongames.com/post/fix_your_timestep/
internal sealed class GameTickScheduler
{
    private const int GameTicksPerSecond = 60;
    private const int MaxTicksPerFrame = 6;

    private const float GameTickDeltaTime = 1f / GameTicksPerSecond;
    private const float DiagnosticTickDeltaTime = 0.5f;

    private readonly FrameTickTimer _gameTicker = new(GameTickDeltaTime);
    private readonly FrameTickTimer _diagnosticTicker = new(DiagnosticTickDeltaTime);

    private SimpleTicker _animationClock = new(5, GameTicksPerSecond);

    private readonly UpdateTickDelegate _onGameTick;
    private readonly UpdateTickDelegate _onUpdateLogTick;

    private float _alpha;
    private float _speed = 1f;


    internal GameTickScheduler(UpdateTickDelegate onGameTick, UpdateTickDelegate onUpdateLogTick)
    {
        _onGameTick = onGameTick;
        _onUpdateLogTick = onUpdateLogTick;
    }

    public float Alpha => _alpha;
    public float FixedDeltaTime => GameTickDeltaTime;

    public void Advance(float deltaTime)
    {
        float dt = deltaTime * _speed;

        _gameTicker.Accumulate(dt);
        _diagnosticTicker.Accumulate(dt);

        int t, tickCounter = 0;
        while (tickCounter < MaxTicksPerFrame && _gameTicker.TryDequeueTick(out t))
        {
            _onGameTick(t, GameTickDeltaTime);
            tickCounter++;
        }

        while (_diagnosticTicker.TryDequeueTick(out t))
        {
            _onUpdateLogTick(t, DiagnosticTickDeltaTime);
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