namespace ConcreteEngine.Core.Time;

//https://gafferongames.com/post/fix_your_timestep/
public sealed class GameTime
{
    private const int MaxTicksPerFrame = 6;
    
    private float _speed = 1f;

    private readonly Action<int> _gameTickAction;
    private readonly Action<int> _fpsTickAction;
    
    private TickTimer _gameTicker = new (1 / 30f);
    private TickTimer _fpsTicker = new (1);

    public GameTime(Action<int> gameTickAction, Action<int> fpsTickAction)
    {
        _gameTickAction = gameTickAction;
        _fpsTickAction = fpsTickAction;
    }

    public void Advance(float deltaTime)
    {
        float dt = deltaTime * _speed;
        
        _gameTicker.Accumulate(dt);
        _fpsTicker.Accumulate(dt);

        int t, tickCounter = 0;
        while (tickCounter < MaxTicksPerFrame && _gameTicker.TryDequeueTick(out t))
        {
            _gameTickAction(t); 
            tickCounter++;
        }
        
        while (_fpsTicker.TryDequeueTick(out t))  _fpsTickAction(t);
    }
}