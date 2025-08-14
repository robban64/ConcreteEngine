namespace ConcreteEngine.Core.Time;

//https://gafferongames.com/post/fix_your_timestep/
public sealed class GameTime
{
    public const int GameTicksPerSecond = 30;
    public const float GameTimeDelta = 1f / GameTicksPerSecond;
    
    private const int MaxTicksPerFrame = 6;
    
    private readonly Action<int> _gameTickAction;
    private readonly Action<int> _fpsTickAction;
    
    private FrameTickTimer _gameTicker = new (GameTimeDelta);
    private FrameTickTimer _fpsTicker = new (1);

    private GameTickTimer _animationClock = new GameTickTimer(5,GameTicksPerSecond);
    
    
    private float _alpha;
    private float _speed = 1f;

    public GameTime(Action<int> gameTickAction, Action<int> fpsTickAction)
    {
        _gameTickAction = gameTickAction;
        _fpsTickAction = fpsTickAction;
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
            _gameTickAction(t);
            GameTickUpdate(t);
            tickCounter++;
        }
        
        while (_fpsTicker.TryDequeueTick(out t))  _fpsTickAction(t);


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