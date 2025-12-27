namespace ConcreteEngine.Engine.Configuration;

internal enum EngineStateLevel : byte
{
    NotStarted = 0,
    LoadingGraphics = 1,
    LoadingAssets = 2,
    InitializeSystem = 3,
    LoadWorld = 4,
    LoadEditor = 5,
    Warmup = 6,
    Running = 7
}