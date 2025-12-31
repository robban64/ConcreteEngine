namespace ConcreteEngine.Engine.Time;

public enum TimeStepKind : byte
{
    None = 0,
    Game,
    Environment,
    Diagnostic,
    System
}