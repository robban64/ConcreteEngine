using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Configuration;

namespace ConcreteEngine.Engine;

internal sealed class EngineTickHub
{
    private const int MaxTicksPerFrame = 6;

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

        EngineTime.GameDelta = _gameTicker.TickDt;
        EngineTime.EnvironmentDelta = _simulationTicker.TickDt;
    }

    public void Reset()
    {
        _gameTicker.Accumulator = 0;
        _simulationTicker.Accumulator = 0;
        _diagnosticTicker.Accumulator = 0;
        _systemTicker.Accumulator = 0;

        EngineTime.FrameId = 0;
        EngineTime.GameTickId = 0;

        EngineTime.GameDelta = _gameTicker.TickDt;
        EngineTime.EnvironmentDelta = _simulationTicker.TickDt;
    }
    
    public void Update(float deltaTime)
    {
        Accumulate(deltaTime);

        // Advance
        if (_systemTicker.DequeueTick(out var tickDt))
            _engine.OnSystemTick(tickDt);

        if (_diagnosticTicker.DequeueTick(out var  diagnosticDt))
            _engine.OnDiagnosticTick(diagnosticDt);

        var tickCounter = 0;
        while (tickCounter < MaxTicksPerFrame && _gameTicker.DequeueTick(out var gameDt))
        {
            ++tickCounter;
            ++EngineTime.GameTickId;
            _engine.OnGameTick(gameDt);
        }

        if (_simulationTicker.DequeueTick(out var simDt))
            _engine.OnSimulateTick(simDt);
        
        EngineTime.AdvanceFrame(deltaTime, _gameTicker.Alpha, _simulationTicker.Alpha);
    }
    
    private void Accumulate(float deltaTime)
    {
        _gameTicker.Accumulate(deltaTime);
        _simulationTicker.Accumulate(deltaTime);
        _diagnosticTicker.Accumulate(deltaTime);
        _systemTicker.Accumulate(deltaTime);
    }


    private static float GetAlpha(double now, double last, float dt)
    {
        var alpha = (float)(now - last) / dt;
        return float.Clamp(alpha, 0f, 1f);
    }
}