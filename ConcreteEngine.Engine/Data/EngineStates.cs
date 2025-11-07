namespace ConcreteEngine.Engine.Data;

internal enum EngineStateLevel : byte
{
    NotStarted = 0,
    LoadingGraphics = 1,
    LoadingAssets = 2,
    InitializeSystem = 3,
    LoadScenes = 4,
    Running = 5
}