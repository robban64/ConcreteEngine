using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Time.Tickers;

namespace ConcreteEngine.Engine.Time;

//https://gafferongames.com/post/fix_your_timestep/
internal sealed class EngineTimeHub
{
    private const int MaxTicksPerFrame = 6;
    private const int GameTicksPerSecond = 60;
    private const int SimulationTickPerSecond = 20;

    private const float GameTickDeltaTime = 1f / GameTicksPerSecond;
    private const float SimulationDeltaTime = 1f / SimulationTickPerSecond;
    private const float DiagnosticTickDeltaTime = 1f / 4;

    private FrameTickTimer _updateTicker = new(GameTickDeltaTime);
    private FrameTickTimer _simulationTicker = new(SimulationDeltaTime);
    private FrameTickTimer _diagnosticTicker = new(DiagnosticTickDeltaTime);

    private double _lastUpdateFinishTime;

    private DebounceTicker _debounceResize;

    private readonly UpdateTickDelegate _onStepTick;
    private readonly UpdateTickDelegate _onSimulationTick;
    private readonly UpdateTickDelegate _onLogTick;

    private readonly Stopwatch _sw = Stopwatch.StartNew();

    internal EngineTimeHub(UpdateTickDelegate onStepTick, UpdateTickDelegate onSimulationTick,
        UpdateTickDelegate onLogTick)
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
        EngineTime.FrameIndex++;
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

    public void Advance(float deltaTime)
    {
        int tickCounter = 0;
        while (tickCounter < MaxTicksPerFrame && _updateTicker.TryDequeueTick(out _))
        {
            tickCounter++;
            _onStepTick(GameTickDeltaTime);
        }

        while (_simulationTicker.TryDequeueTick(out _))
            _onSimulationTick(SimulationDeltaTime);

        while (_diagnosticTicker.TryDequeueTick(out _))
            _onLogTick(DiagnosticTickDeltaTime);

        _lastUpdateFinishTime = _sw.Elapsed.TotalSeconds;
    }


    public void BeginDebounceResize(int ticks)
    {
        if (_debounceResize.TicksLeft == 0)
            _debounceResize.Debounce(ticks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryTriggerDebounceResize() => _debounceResize.TicksLeft > 0 && _debounceResize.Tick();


    public float GetGameAlpha(double now)
    {
        var alpha = (float)((now - _lastUpdateFinishTime) / GameTickDeltaTime);
        return float.Clamp(alpha, 0f, 1f);
    }

    public float GetSimulationAlpha(double now)
    {
        var alpha = (float)((now - _lastUpdateFinishTime) / SimulationDeltaTime);
        return float.Clamp(alpha, 0f, 1f);
    }
}