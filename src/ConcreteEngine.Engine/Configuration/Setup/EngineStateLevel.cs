namespace ConcreteEngine.Engine.Configuration;

internal enum EngineSetupState : byte
{
    NotStarted = 0,
    LoadAssets = 1,
    SetupRenderer = 2,
    SetupInternal = 3,
    LoadWorld = 4,
    LoadScene = 5,
    LoadEditor = 6,
    Warmup = 7,
    Final = 8,
    Running = 9
}