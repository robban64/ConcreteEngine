using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Time;
using ConcreteEngine.Engine.Configuration;

namespace ConcreteEngine.Engine.Time;

internal sealed class EngineTimeHub
{
    private const int MaxTicksPerFrame = 6;

    private DebounceTicker _debounceResize;

    //private double _lastUpdateFinishTime;

    private FrameTickTimer _gameTicker;
    private FrameTickTimer _environmentTicker;
    private FrameTickTimer _diagnosticTicker;
    private FrameTickTimer _systemTicker;

    private readonly Action<float> _onGameTick;
    private readonly Action<float> _onEnvironmentTick;
    private readonly Action<float> _onLogTick;
    private readonly Action<float> _onSystemTick;

    //private readonly Stopwatch _sw;

    internal EngineTimeHub(
        Action<float> onGameTick,
        Action<float> onEnvironmentTick,
        Action<float> onLogTick,
        Action<float> onSystemTick)
    {
        var sim = EngineSettings.Instance.Simulation;
        _gameTicker = new FrameTickTimer(1.0f/sim.GameSimRate);
        _environmentTicker = new FrameTickTimer(1.0f/sim.EnvironmentSimRate);
        _diagnosticTicker = new FrameTickTimer(1.0f/sim.DiagnosticSimRate);
        _systemTicker = new FrameTickTimer(1.0f);

        _onLogTick = onLogTick;
        _onEnvironmentTick = onEnvironmentTick;
        _onGameTick = onGameTick;
        _onSystemTick = onSystemTick;

        EngineTime.GameTickDeltaTime = _gameTicker.TickDt;
        EngineTime.EnvironmentDeltaTime = _environmentTicker.TickDt;

        //_sw = Stopwatch.StartNew();
        //_lastUpdateFinishTime = _sw.Elapsed.TotalSeconds;
    }

    public void BeginFrame(float deltaTime)
    {
        EngineTime.FrameId++;
        EngineTime.Timestamp = TimeUtils.GetFastTimestamp();
        EngineTime.DeltaTime = deltaTime;
        EngineTime.Time += deltaTime;

        //var now = _sw.Elapsed.TotalSeconds;
        //GetAlpha(now, _lastUpdateFinishTime, _gameTicker.TickDt);
        //(now, _lastUpdateFinishTime, _gameTicker.TickDt);
        EngineTime.GameAlpha = _gameTicker.Alpha;
        EngineTime.EnvironmentAlpha = _environmentTicker.Alpha;

        EngineTime.Fps = deltaTime > 0 ? 1.0f / deltaTime : 0.0f;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Accumulate(float deltaTime)
    {
        _gameTicker.Accumulate(deltaTime);
        _environmentTicker.Accumulate(deltaTime);
        _diagnosticTicker.Accumulate(deltaTime);
        _systemTicker.Accumulate(deltaTime);
    }

    public void Advance()
    {
        var tickCounter = 0;

        while (tickCounter < MaxTicksPerFrame && _gameTicker.DequeueTick())
        {
            tickCounter++;
            _onGameTick(_gameTicker.TickDt);
        }

        while (_environmentTicker.DequeueTick())
            _onEnvironmentTick(_environmentTicker.TickDt);

        if (_diagnosticTicker.DequeueTick())
            _onLogTick(_diagnosticTicker.TickDt);

        if (_systemTicker.DequeueTick())
            _onSystemTick(_systemTicker.TickDt);

        // _lastUpdateFinishTime = _sw.Elapsed.TotalSeconds;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debounce(int ticks) => _debounceResize.Debounce(ticks);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryTriggerDebounceResize() => _debounceResize.TicksLeft > 0 && _debounceResize.Tick();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float GetAlpha(double now, double last, float dt)
    {
        var alpha = (float)((now - last)) / dt;
        return float.Clamp(alpha, 0f, 1f);
    }
}