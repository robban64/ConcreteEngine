namespace ConcreteEngine.Editor.Definitions;

public enum EventKey : byte
{
    EditorStarted,
    EditorStopped,
    
    StateTransition,

    CategoryChanged,

    SelectionChanged,
    SelectionUpdated,
    SelectionAction,

    CommitData,
    GraphicsSetting,
}