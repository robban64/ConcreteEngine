namespace ConcreteEngine.Editor.Definitions;

internal enum EventKey : byte
{
    EditorStarted,
    EditorStopped,

    CategoryChanged,

    SelectionChanged,
    SelectionUpdated,
    SelectionAction,

    CommitVisualData,
    GraphicsSetting,
}