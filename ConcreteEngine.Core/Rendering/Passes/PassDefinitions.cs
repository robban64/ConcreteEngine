namespace ConcreteEngine.Core.Rendering.Passes;


public readonly record struct PassId(int Value)
{
    public static implicit operator int(PassId id) => id.Value;
    public static implicit operator byte(PassId id) => (byte)id.Value;

}

public enum PassOpKind : byte
{
    Normal = 0,
    Resolve = 1,
    Fsq = 2,
    Screen = 3
}

public readonly record struct ApplyPassReturn(PassOpKind OpKind)
{
    public static ApplyPassReturn NormalPassResult() => new(PassOpKind.Normal);
    public static ApplyPassReturn ResolveTargetResult() => new(PassOpKind.Resolve);
    public static ApplyPassReturn FsqPassResult() => new(PassOpKind.Fsq);
    public static ApplyPassReturn ScreenPassResult() => new(PassOpKind.Screen);
}