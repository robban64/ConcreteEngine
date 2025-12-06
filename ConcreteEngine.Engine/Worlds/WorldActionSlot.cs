#region

using ConcreteEngine.Engine.Worlds.Entities;

#endregion

namespace ConcreteEngine.Engine.Worlds;

internal static class WorldActionSlot
{
    public static EntityId SelectedEntityId;

    public static bool IsDirty { get; private set; }

    public static bool HasPendingSlot<T>(long gen) where T : unmanaged => gen <= ValueSlot<T>.Generation;
    
    public static ref readonly T ReadSlot<T>() where T : unmanaged => ref ValueSlot<T>.Data; 


    public static void SetSlot<T>(long generation, in T data) where T : unmanaged
    {
        IsDirty = true;
        ValueSlot<T>.Generation = generation;
        ValueSlot<T>.Data = data;
    }

    public static void ClearDirty() => IsDirty = false;

    private static class ValueSlot<T> where T : unmanaged
    {
        public static T Data;
        public static long Generation = -1;
    }
    
    
    /*
    public static bool TryReadSlot<T>(long gen, out T data)
    {
        data = ValueSlot<T>.Data;
        return gen <= ValueSlot<T>.Generation;
    }

    public static ref T WriteSlot<T>(long generation)
    {
        IsDirty = true;
        ValueSlot<T>.Generation = generation;
        return ref ValueSlot<T>.Data;
    }*/
}