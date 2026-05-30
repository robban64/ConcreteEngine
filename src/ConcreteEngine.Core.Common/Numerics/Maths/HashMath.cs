using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class HashMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint HashFnv(byte* data, uint length)
    {
        uint hash = 2166136261;
        for (int i = 0; i < length; i++)
            hash = (hash ^ data[i]) * 16777619;
        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HashFnv(Span<byte> data)
    {
        uint hash = 2166136261;
        for (int i = 0; i < data.Length; i++)
            hash = (hash ^ data[i]) * 16777619;
        return hash;
    }

}