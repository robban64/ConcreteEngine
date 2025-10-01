namespace ConcreteEngine.Core.Time;

//https://gafferongames.com/post/fix_your_timestep/
internal sealed class GameTime
{
    private const int GameTicksPerSecond = 30;
    private const int MaxTicksPerFrame = 6;

    private FrameTickTimer _gameTicker = new(1f / GameTicksPerSecond);
    private FrameTickTimer _fpsTicker = new(1f);
    private GameTickTimer _animationClock = new(5, GameTicksPerSecond);

    private readonly GameTimeTickDelegate _onGameTick;
    private readonly GameTimeTickDelegate _onUpdateLogTick;


    private float _alpha;
    private float _speed = 1f;

    internal GameTime(GameTimeTickDelegate onGameTick, GameTimeTickDelegate onUpdateLogTick)
    {
        _onGameTick = onGameTick;
        _onUpdateLogTick = onUpdateLogTick;
    }

    public float Alpha => _alpha;

    public void Advance(float deltaTime)
    {
        float dt = deltaTime * _speed;

        _gameTicker.Accumulate(dt);
        _fpsTicker.Accumulate(dt);

        int t, tickCounter = 0;
        while (tickCounter < MaxTicksPerFrame && _gameTicker.TryDequeueTick(out t))
        {
            _onGameTick(t);
            GameTickUpdate(t);
            tickCounter++;
        }

        while (_fpsTicker.TryDequeueTick(out t)) _onUpdateLogTick(t);


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