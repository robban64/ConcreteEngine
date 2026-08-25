using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Configuration;

namespace ConcreteEngine.Engine;

internal sealed class EngineTickHub
{
    private const int MaxTicksPerFrame = 6;
    private const float SystemDt = 0.25f;
    
    private readonly GameEngine _engine;

    private FrameAccumulator _gameTicker;
    private FrameAccumulator _simulationTicker;
    private FrameAccumulator _diagnosticTicker;
    private FrameAccumulator _systemTicker;

    internal EngineTickHub(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        var sim = EngineSettings.Current.Simulation;
        _gameTicker = new FrameAccumulator(1.0f / sim.GameSimRate);
        _simulationTicker = new FrameAccumulator(1.0f / sim.EnvironmentSimRate);
        _diagnosticTicker = new FrameAccumulator(1.0f / sim.DiagnosticSimRate);
        _systemTicker = new FrameAccumulator(0.25f);

        _engine = engine;

        EngineTime.GameDelta = (float)_gameTicker.TickDt;
        EngineTime.SimulationDelta = (float)_simulationTicker.TickDt;
    }

    public void Reset()
    {
        _gameTicker.Accumulator = 0;
        _simulationTicker.Accumulator = 0;
        _diagnosticTicker.Accumulator = 0;
        _systemTicker.Accumulator = 0;

        EngineTime.FrameId = 0;
        EngineTime.GameTickId = 0;

        EngineTime.GameDelta = (float)_gameTicker.TickDt;
        EngineTime.SimulationDelta = (float)_simulationTicker.TickDt;
    }

    public void Update(double deltaTime)
    {
        Accumulate(deltaTime);

        // Advance
        if (_systemTicker.TryDrainTick())
            _engine.OnSystemTick(SystemDt);

        if (_diagnosticTicker.TryDrainTick())
            _engine.OnDiagnosticTick(_diagnosticTicker.TickDt);

        var updateCounter = 0;
        while (++updateCounter < MaxTicksPerFrame && _gameTicker.DequeueTick(out var gameDt))
        {
            ++EngineTime.GameTickId;
            _engine.OnGameTick(gameDt);
        }

        var simCounter = 0;
        while (++simCounter < MaxTicksPerFrame && _simulationTicker.DequeueTick(out var simDt))
        {
            ++simCounter;
            _engine.OnSimulateTick(simDt);
        }

        EngineTime.AdvanceFrame(deltaTime, _gameTicker.Alpha, _simulationTicker.Alpha);
    }

    private void Accumulate(double deltaTime)
    {
        _gameTicker.Accumulate(deltaTime);
        _simulationTicker.Accumulate(deltaTime);
        _diagnosticTicker.Accumulate(deltaTime);
        _systemTicker.Accumulate(deltaTime);
    }

}