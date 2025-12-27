namespace ConcreteEngine.Engine.Time;

internal delegate void UpdateTickDelegate(float deltaTime);
/*
public readonly struct UpdateTickArgs(int tick, float deltaTime, float alpha)
{
    public readonly int Tick  = tick;
    public readonly float DeltaTime = deltaTime;
    public readonly float Alpha  = alpha;

    public UpdateTickArgs WithAlpha(float alpha) => new(Tick, DeltaTime, alpha);
}
*/