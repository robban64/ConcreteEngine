using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Specs.Visuals;
using ConcreteEngine.Editor.Data;

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
        internal static void Touch()
        {
            {
                var view = GetView();
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