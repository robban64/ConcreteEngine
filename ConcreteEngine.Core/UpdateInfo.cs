namespace ConcreteEngine.Core;

public readonly record struct UpdateInfo(int GameTick, float Fps, float DeltaTime, float Alpha);