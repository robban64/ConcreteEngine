using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Time;

namespace ConcreteEngine.Engine.Time;

internal sealed class EngineTimeHub
{
    private const int MaxTicksPerFrame = 6;
    private const int GameTicksPerSecond = 60;
    private const int SimulationTickPerSecond = 40;

    private const float GameTickDeltaTime = 1f / GameTicksPerSecond;
    private const float SimulationDeltaTime = 1f / SimulationTickPerSecond;
    private const float DiagnosticTickDeltaTime = 1f / 4;


    private FrameTickTimer _updateTicker = new(GameTickDeltaTime);
    private FrameTickTimer _simulationTicker = new(SimulationDeltaTime);
    private FrameTickTimer _diagnosticTicker = new(DiagnosticTickDeltaTime);

    private DebounceTicker _debounceResize;

    private double _lastUpdateFinishTime;


    private readonly Action<float> _onStepTick;
    private readonly Action<float> _onSimulationTick;
    private readonly Action<float> _onLogTick;


    private readonly Stopwatch _sw = Stopwatch.StartNew();

    internal EngineTimeHub(
        Action<float> onStepTick,
        Action<float> onSimulationTick,
        Action<float> onLogTick)
    {
        _onSimulationTick = onSimulationTick;
        _onLogTick = onLogTick;
        _onStepTick = onStepTick;

        EngineTime.GameTickDeltaTime = GameTickDeltaTime;
        EngineTime.SimulationDeltaTime = SimulationDeltaTime;
        EngineTime.DiagnosticTickDeltaTime = DiagnosticTickDeltaTime;

        _lastUpdateFinishTime = _sw.Elapsed.TotalSeconds;
    }

    public void UpdateFrame(float deltaTime)
    {
        EngineTime.FrameId++;
        EngineTime.Timestamp = TimeUtils.GetFastTimestamp();
        EngineTime.DeltaTime = deltaTime;
        EngineTime.Time += deltaTime;

        double now = _sw.Elapsed.TotalSeconds;
        EngineTime.GameAlpha = GetGameAlpha(now);
        EngineTime.SimulationAlpha = GetSimulationAlpha(now);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Accumulate(float deltaTime)
    {
        _updateTicker.Accumulate(deltaTime);
        _simulationTicker.Accumulate(deltaTime);
        _diagnosticTicker.Accumulate(deltaTime);
    }

    public void Advance()
    {
        int tickCounter = 0;
        while (tickCounter < MaxTicksPerFrame && _updateTicker.DequeueTick())
        {
            tickCounter++;
            _onStepTick(GameTickDeltaTime);
        }

        while (_simulationTicker.DequeueTick())
            _onSimulationTick(SimulationDeltaTime);

        if (_diagnosticTicker.DequeueTick())
            _onLogTick(DiagnosticTickDeltaTime);

        _lastUpdateFinishTime = _sw.Elapsed.TotalSeconds;
    }

    public void Debounce(int ticks) => _debounceResize.Debounce(ticks);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryTriggerDebounceResize() => _debounceResize.TicksLeft > 0 && _debounceResize.Tick();


    private float GetGameAlpha(double now)
    {
        var alpha = (float)((now - _lastUpdateFinishTime) / GameTickDeltaTime);
        return float.Clamp(alpha, 0f, 1f);
    }

    private float GetSimulationAlpha(double now)
    {
        var alpha = (float)((now - _lastUpdateFinishTime) / SimulationDeltaTime);
        return float.Clamp(alpha, 0f, 1f);
    }
}