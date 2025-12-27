namespace ConcreteEngine.Core.Diagnostics;

public readonly record struct MetricHeader(ushort Flags = 0, byte Kind = 0, byte State = 0)
{
    public static MetricHeader FromKind(byte kind) => new(Kind: kind);
}