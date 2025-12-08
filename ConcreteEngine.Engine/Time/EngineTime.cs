namespace ConcreteEngine.Engine.Time;

public static class EngineTime
{
    public static UpdateTickArgs GameTime { get; internal set; }
    public static UpdateTickArgs SimulationDeltaTime { get; internal set; } 
    public static UpdateTickArgs DiagnosticDeltaTime { get; internal set; } 
}