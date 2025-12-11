#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Shared.Diagnostics;
using ConcreteEngine.Shared.Input;

#endregion

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Editor.Store;

public static class EditorDataStore
{
    public static class Input
    {
        public static MouseDataState MouseState;
        public static EditorSelectionState EditorSelection;

        internal static void Reset()
        {
            MouseState = default;
            EditorSelection = default;
        }
    }

    public static class State
    {
        public static EditorId SelectedEntity;
        public static EntityDataState EntityState;
        public static EditorParticleState ParticleState;
    }

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