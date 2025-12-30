namespace ConcreteEngine.Engine.Configuration;

internal enum EngineStateLevel : byte
{
    NotStarted ,
    LoadingAssets ,
    LoadingGraphics ,
    InitializeSystem ,
    LoadWorld ,
    LoadEditor ,
    Warmup ,
    Running
}