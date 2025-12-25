using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Shared.Visuals;

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Editor.Store;

public static class EditorDataStore
{
    internal static EditorId SelectedSceneObject;

    internal static EditorId SelectedEntity;
    internal static EditorEntityState EntityState;
    internal static EditorParticleState ParticleState;
    internal static EditorAnimationState AnimationState;

    internal static class Slot<T> where T : unmanaged
    {
        public static T State;
        public static long Generation;

        public static EditorSlot<T> GetView() => new(ref State, ref Generation);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Touch()
        {
            {
                EditorSlot<T> view = GetView();
                view.Gen = Unsafe.SizeOf<T>();
                view.State = default;
            }

            Generation = 0;
            State = default;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void WarmUp()
    {
        Slot<EditorCameraState>.Touch();
        Slot<WorldParamsData>.Touch();
    }
}