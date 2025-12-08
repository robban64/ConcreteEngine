using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Shared.Input;

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Editor.Store;

public static class EditorDataStore
{
    public static class Input
    {
        public static MouseDataState MouseState;
        public static EditorSelectionState EditorSelection;
    }
    
    public static class State
    {
        public static EditorId SelectedId;
        public static EntityDataState EntityState; 
    }

    public static class Slot<T> where T : unmanaged
    {
        public static T Data;
        public static EditorSlotState SlotState;
    }
}