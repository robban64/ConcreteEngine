using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Engine.Configuration;

namespace ConcreteEngine.Engine.Time;

internal sealed class EngineTickHub
{
    private const int MaxTicksPerFrame = 6;


    private FrameTickTimer _gameTicker;
    private FrameTickTimer _environmentTicker;
    private FrameTickTimer _diagnosticTicker;
    private FrameTickTimer _systemTicker;

    private readonly Action<float> _onGameTick;
    private readonly Action<float> _onEnvironmentTick;
    private readonly Action<float> _onLogTick;
    private readonly Action<float> _onSystemTick;

    internal EngineTickHub(
        Action<float> onGameTick,
        Action<float> onEnvironmentTick,
        Action<float> onLogTick,
        Action<float> onSystemTick)
    {
        var sim = EngineSettings.Instance.Simulation;
        _gameTicker = new FrameTickTimer(1.0f / sim.GameSimRate);
        _environmentTicker = new FrameTickTimer(1.0f / sim.EnvironmentSimRate);
        _diagnosticTicker = new FrameTickTimer(1.0f / sim.DiagnosticSimRate);
        _systemTicker = new FrameTickTimer(0.25f);

        _onLogTick = onLogTick;
        _onEnvironmentTick = onEnvironmentTick;
        _onGameTick = onGameTick;
        _onSystemTick = onSystemTick;

        EngineTime.GameDelta = _gameTicker.TickDt;
        EngineTime.EnvironmentDelta = _environmentTicker.TickDt;

        //_sw = Stopwatch.StartNew();
        //_lastUpdateFinishTime = _sw.Elapsed.TotalSeconds;
    }

    public void Reset()
    {
        _gameTicker.Accumulator = 0;
        _environmentTicker.Accumulator = 0;
        _diagnosticTicker.Accumulator = 0;
        _systemTicker.Accumulator = 0;

        EngineTime.FrameId = 0;
        EngineTime.GameTickId = 0;

        EngineTime.GameDelta = _gameTicker.TickDt;
        EngineTime.EnvironmentDelta = _environmentTicker.TickDt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceFrame(float deltaTime)
    {
        EngineTime.AdvanceFrame(deltaTime, _gameTicker.Alpha, _environmentTicker.Alpha);
    }

    public void Update(float deltaTime)
    {
        _gameTicker.Accumulate(deltaTime);
        _environmentTicker.Accumulate(deltaTime);
        _diagnosticTicker.Accumulate(deltaTime);
        _systemTicker.Accumulate(deltaTime);

        // Advance
        var tickCounter = 0;

        while (tickCounter < MaxTicksPerFrame && _gameTicker.DequeueTick())
        {
            tickCounter++;
            EngineTime.GameTickId++;
            _onGameTick(_gameTicker.TickDt);
        }

        while (_environmentTicker.DequeueTick())
            _onEnvironmentTick(_environmentTicker.TickDt);

        if (_diagnosticTicker.DequeueTick())
            _onLogTick(_diagnosticTicker.TickDt);

        if (_systemTicker.DequeueTick())
            _onSystemTick(_systemTicker.TickDt);
    }

    private static float GetAlpha(double now, double last, float dt)
    {
        var alpha = (float)(now - last) / dt;
        return float.Clamp(alpha, 0f, 1f);
    }
}