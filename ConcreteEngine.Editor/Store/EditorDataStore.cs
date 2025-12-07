using ConcreteEngine.Editor.Components.Data;
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
        public static EditorMouseAction MouseAction;
        
        public static EntitySelectionState EntitySelection;
    }
    
    public static class StateSlot
    {
        public static EntityDataState SelectedEntityState; 
    }

    public static class Slot<T> where T : unmanaged
    {
        public static T Data;
        public static EditorSlotState State;
    }
}