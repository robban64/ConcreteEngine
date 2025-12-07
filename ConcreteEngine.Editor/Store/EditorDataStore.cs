using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Shared.RenderData;

namespace ConcreteEngine.Editor.Store;

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

public static class EditorDataStore
{
    public static EditorDataState<CameraDataState> CameraData;
    public static EditorDataState<EntityDataState> EntityData;
    public static EditorDataState<WorldParamsData> WorldData;

    static EditorDataStore()
    {
        Slot<CameraDataState>.Data = default;
        Slot<EntityDataState>.Data = default;
        Slot<WorldParamsData>.Data = default;
    }


    public static class Slot<T> where T : unmanaged
    {
        public static T Data;
        public static long Generation;
    }
}