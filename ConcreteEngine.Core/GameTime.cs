namespace ConcreteEngine.Core;

public sealed class GameTime
{
    private int _simulationTick = 0;
    private float _accumulatorForTick = 0;
    
    public float DeltaTime { get; private set; }
    public float FramesPerSecond { get; private set; }

    public float SimulationDeltaTime { get; private set; } = 1f / 50f; //50Hz
    
}