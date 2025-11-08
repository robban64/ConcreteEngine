using System.Runtime.CompilerServices;

namespace ConcreteEngine.Shared.Diagnostics;

public readonly record struct MetricHeader(ushort Flags = 0, byte Kind = 0, byte State = 0)
{
    public static MetricHeader FromKind<TEnum>(TEnum kind) where TEnum : unmanaged, Enum 
        => new(Kind: Unsafe.As<TEnum, byte>(ref kind));
}