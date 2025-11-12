namespace ConcreteEngine.Engine.Worlds;

internal static class WorldActionSlot
{
    public static bool IsDirty { get; private set; }
    public static bool TryReadSlot<T>(long gen, out T data) where T : unmanaged
    {
        data =  Slot<T>.Data;
        return gen <= Slot<T>.Generation;
    }

    public static ref T WriteSlot<T>(long generation) where T : unmanaged
    {
        IsDirty = true;
        Slot<T>.Generation = generation;
        return ref Slot<T>.Data;
    }
    
    public static void ClearDirty() => IsDirty = false;

    private static class Slot<T> where T : unmanaged
    {
        public static T Data;
        public static long Generation = -1;
    }
}