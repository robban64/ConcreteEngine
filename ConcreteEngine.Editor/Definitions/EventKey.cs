namespace ConcreteEngine.Editor.Definitions;

internal enum TransitionKey
{
    Enter,
    Leave,
    Refresh
}

internal enum EventKey
{
    EditorStarted,
    EditorStopped,

    CategoryChanged,

    SelectionChanged,
    SelectionUpdated,
    SelectionAction,
    
    MouseSelectEntity,
    MouseDragEntityTerrain,
}