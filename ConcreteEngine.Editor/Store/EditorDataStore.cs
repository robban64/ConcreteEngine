#region

using ConcreteEngine.Editor.Data;

#endregion

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Editor.Store;

public static class EditorDataStore
{
    internal static EditorId SelectedEntity;
    internal static EditorEntityState EntityState;
    internal static EditorParticleState ParticleState;
    internal static EditorAnimationState AnimationState;

    internal static class Slot<T> where T : unmanaged
    {
        public static T State;
        public static long Generation;
        public static EditorSlot<T> GetView() => new(ref State, ref Generation);
    }
}