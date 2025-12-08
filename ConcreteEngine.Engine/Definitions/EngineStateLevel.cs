namespace ConcreteEngine.Engine.Definitions;

internal enum EngineStateLevel : byte
{
    NotStarted = 0,
    LoadingGraphics = 1,
    LoadingAssets = 2,
    InitializeSystem = 3,
    LoadScenes = 4,
    LoadEditor = 5,
    Running = 6
}