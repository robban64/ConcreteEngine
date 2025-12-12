#region

using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Shared.Diagnostics;
using ConcreteEngine.Shared.Input;

#endregion

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Editor.Store;

public static class EditorDataStore
{
    public static EditorId SelectedEntity;
    public static EditorEntityState EntityState;
    public static EditorParticleState ParticleState;
    public static EditorAnimationState AnimationState;

    public static class Slot<T> where T : unmanaged
    {
        public static T Data;
        public static EditorSlotState SlotState;
    }

    public static class MetricState
    {
        public static RenderInfoSample FrameSample;
        public static FrameMetric FrameMetrics;
    }
}