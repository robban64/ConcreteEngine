using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Editor.Core;

public static class StoreHub
{
    internal static SceneObjectId SelectedId => SelectedProxy?.Id ?? SceneObjectId.Empty;
    internal static SceneObjectProxy? SelectedProxy;
    
    internal static class Slot<T> where T : unmanaged
    {
        public static T State;
        public static long Generation;

        public static SlotView<T> GetView() => new(ref State, ref Generation);
    }

}