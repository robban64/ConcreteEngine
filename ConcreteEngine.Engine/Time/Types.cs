namespace ConcreteEngine.Engine.Time;

internal delegate void UpdateTickDelegate(UpdateTickArgs args);

internal readonly struct UpdateTickArgs(int tick, float fixedDt, float alpha)
{
    public readonly int Tick  = tick;
    public readonly float FixedDt = fixedDt;
    public readonly float Alpha  = alpha;
}