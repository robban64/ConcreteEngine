using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics;



[Flags]
public enum BufferAccess : byte
{
    None = 0,
    MapRead = 1 << 0,
    MapWrite = 1 << 1,
    Persistent = 1 << 2,
    Coherent = 1 << 3
}

internal static class GfxFlags
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has(this BufferAccess a, BufferAccess b) => (a & b) != 0;
    
    
}