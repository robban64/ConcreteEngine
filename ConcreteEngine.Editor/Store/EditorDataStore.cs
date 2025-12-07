using ConcreteEngine.Editor.Components.Data;

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Editor.Store;
/*
public sealed class EditorDataState<T> where T : unmanaged
{
    public T Data;
    internal T DataState;
    public long Generation { get; private set; }

    public void Set(long generation, in T data)
    {
        Data = data;
        Generation = generation;
    }
}
*/

public static class EditorDataStore
{
    
    
    
    public static bool HasPendingSlot<T>(long gen, out long editorGen) where T : unmanaged
    {
        editorGen = ValueSlot<T>.Generation;
        return ValueSlot<T>.IsDirty || editorGen <= gen;
    }

    public static ref readonly T ReadSlot<T>() where T : unmanaged => ref ValueSlot<T>.Data;

    public static void OverwriteSlot<T>(long generation, in T data) where T : unmanaged
        => ValueSlot<T>.Set(generation, in data, false);

    internal static void WriteSlot<T>(in T data) where T : unmanaged
    {
        ValueSlot<T>.Data = data;
        ValueSlot<T>.IsDirty = true;
    }

    private static class ValueSlot<T> where T : unmanaged
    {
        public static T Data;
        public static long Generation;
        public static bool IsDirty;

        public static void Set(long generation, in T value, bool isDirty)
        {
            Data = value;
            Generation = generation;
            IsDirty = isDirty;
        }
    }
}