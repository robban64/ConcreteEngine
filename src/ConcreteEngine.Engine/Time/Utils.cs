using ConcreteEngine.Engine.Configuration;

namespace ConcreteEngine.Engine.Time;

public static class EngineTimeExtensions
{
    public static int ToRate(this TimeStepKind step)
    {
        var sim = EngineSettings.Instance.Simulation;
        return step switch
        {
            TimeStepKind.None => EngineSettings.Instance.Display.FrameRate,
            TimeStepKind.Game => sim.GameSimRate,
            TimeStepKind.Environment => sim.EnvironmentSimRate,
            TimeStepKind.Diagnostic => sim.DiagnosticSimRate,
            TimeStepKind.System => EngineTime.SystemTickRate,
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, null)
        };
    }
}