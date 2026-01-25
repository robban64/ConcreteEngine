using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Editor.Core;

internal static class DataStore
{
    public static NativeArray<byte> SceneInputBuffer32 = new(32, true);
    public static NativeArray<byte> InputCliBuffer128 = new(128, true);
    
    public static NativeArray<byte> WriterBuffer256 = new(256, true);

}