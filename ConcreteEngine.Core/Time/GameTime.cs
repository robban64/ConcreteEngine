namespace ConcreteEngine.Core.Time;

//https://gafferongames.com/post/fix_your_timestep/
public sealed class GameTime
{
    // Update
    private const int GameTicksPerSecond = 30;
    private const int MaxTicksPerFrame = 6;

    private FrameTickTimer _gameTicker = new(1f / GameTicksPerSecond);
    private FrameTickTimer _fpsTicker = new(1f);
    private GameTickTimer _animationClock = new(5, GameTicksPerSecond);
    
    private readonly Action<int> _onGameTick;
    private readonly Action<int> _onUpdateLogTick;

    // Render
    public const int GpuUploadTicksPerSecond = 20;  // 20 Hz
    public const int GpuDisposeTicksPerSecond = 1;  // 1 Hz
    public const int ParticleLodTicksPerSecond = 15; // 15 Hz
    
    private readonly FrameTickTimer _gpuUploadTicker = new(1f / GpuUploadTicksPerSecond);
    private readonly FrameTickTimer _gpuDisposeTicker = new(1f / GpuDisposeTicksPerSecond);
    private readonly FrameTickTimer _particleLodTicker = new(1f / ParticleLodTicksPerSecond);
    
    private readonly Action<int> _onGpuUpload;
    private readonly Action<int> _onGpuDispose;
    private readonly Action<int> _onGpuEffect;

    private float _alpha;
    private float _speed = 1f;

    public GameTime(Action<int> onGameTick, Action<int> onUpdateLogTick)
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