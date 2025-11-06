namespace Core.DebugTools.Data;

public enum EditorRequestAction
{
    Reload,
    Set,
    Create,
    Delete,
}

public readonly record struct EditorShadowPayload(int Size, bool Enabled, EditorRequestAction RequestAction);

public readonly record struct EditorShaderPayload(string Name, EditorRequestAction RequestAction);

public readonly struct EditorTransformPayload(int entityId, in EntityEditorTransform transform)
{
    public readonly int EntityId = entityId;
    public readonly EntityEditorTransform Transform = transform;
}