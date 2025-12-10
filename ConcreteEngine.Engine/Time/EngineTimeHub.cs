#region

using System.Diagnostics;
using ConcreteEngine.Engine.Time.Tickers;

#endregion

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

    private readonly FrameTickTimer _updateTicker = new(GameTickDeltaTime);
    private readonly FrameTickTimer _simulationTicker = new(SimulationDeltaTime);
    private readonly FrameTickTimer _diagnosticTicker = new(DiagnosticTickDeltaTime);

    private readonly Stopwatch _sw = Stopwatch.StartNew();

    private readonly UpdateTickDelegate _onStepTick;
    private readonly UpdateTickDelegate _onSimulationTick;
    private readonly UpdateTickDelegate _onLogTick;

    private double _lastUpdateFinishTime;

    public DebounceTicker? DebounceTicker { get; set; } = null;


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


    public void Advance(float deltaTime)
    {
        _updateTicker.Accumulate(deltaTime);
        _simulationTicker.Accumulate(deltaTime);
        _diagnosticTicker.Accumulate(deltaTime);

        int tickCounter = 0;
        while (tickCounter < MaxTicksPerFrame && _updateTicker.TryDequeueTick(out _))
        {
            tickCounter++;
            _onStepTick(GameTickDeltaTime);
        }
        // _onStepTick(EngineTime.GameTime);


        while (_simulationTicker.TryDequeueTick(out _))
            _onSimulationTick(SimulationDeltaTime);

        while (_diagnosticTicker.TryDequeueTick(out _))
            _onLogTick(DiagnosticTickDeltaTime);

        _lastUpdateFinishTime = _sw.Elapsed.TotalSeconds;
    }
    
    public float GetGameAlpha()
    {
        double now = _sw.Elapsed.TotalSeconds;
        var alpha = (float)((now - _lastUpdateFinishTime) / GameTickDeltaTime);
        return float.Clamp(alpha, 0f, 1f);
    }
    
    public float GetSimulationAlpha()
    {
        double now = _sw.Elapsed.TotalSeconds;
        var alpha = (float)((now - _lastUpdateFinishTime) / SimulationDeltaTime);
        return float.Clamp(alpha, 0f, 1f);
    }
}