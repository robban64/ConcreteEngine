using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Editor.Core;

public static class EditorDataStore
{
    internal static SceneObjectId SelectedSceneObj;
    internal static SceneObjectView? SceneObjectView;
    
    internal static class Slot<T> where T : unmanaged
    {
        public static T State;
        public static long Generation;

        public static EditorSlot<T> GetView() => new(ref State, ref Generation);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void WarmUp()
    {
        Slot<EditorCameraState>.GetView().Gen = 0;
        Slot<EditorCameraState>.GetView().State = default;

        Slot<EditorVisualState>.GetView().Gen = 0;
        Slot<EditorVisualState>.GetView().State = default;
    }
}