namespace ConcreteEngine.Engine.Time;

internal delegate void UpdateTickDelegate(UpdateTickerArgs args);

internal readonly struct UpdateTickerArgs(int tick, float fixedDt, float alpha)
{
    public readonly int Tick  = tick;
    public readonly float FixedDt = fixedDt;
    public readonly float Alpha  = alpha;
}